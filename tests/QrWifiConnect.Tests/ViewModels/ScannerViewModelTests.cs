using QrWifiConnect.Models;
using QrWifiConnect.Services;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.ViewModels;

public sealed class ScannerViewModelTests
{
    private readonly QrParserService _parser = new();
    private readonly FakeCameraPermissionService _permissions = new();
    private readonly FakeNavigationService _navigation = new();

    private ScannerViewModel CreateSut() =>
        new(_parser, _permissions, _navigation);

    // --- Non-WiFi QR is ignored ---

    [Fact]
    public async Task OnQrDetected_NonWifiQr_DoesNotNavigate()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        await vm.OnQrDetectedAsync("https://example.com");

        Assert.Empty(_navigation.History);
        Assert.Equal(ScanState.Scanning, vm.ScanState);
    }

    [Fact]
    public async Task OnQrDetected_PlainText_DoesNotNavigate()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        await vm.OnQrDetectedAsync("just a plain string");

        Assert.Empty(_navigation.History);
    }

    // --- Valid WiFi QR navigates to confirmation ---

    [Fact]
    public async Task OnQrDetected_ValidWifiQr_NavigatesToConfirmation()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        await vm.OnQrDetectedAsync("WIFI:T:WPA;S:HomeNet;P:pass123;;");

        Assert.Single(_navigation.History);
        Assert.Equal("confirmation", _navigation.LastRoute);
        Assert.NotNull(_navigation.LastParams);
        Assert.True(_navigation.LastParams!.ContainsKey("credential"));
        var credential = _navigation.LastParams["credential"] as WifiCredential;
        Assert.NotNull(credential);
        Assert.Equal("HomeNet", credential.Ssid);
    }

    [Fact]
    public async Task OnQrDetected_ValidWifiQr_PausesScanState()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        await vm.OnQrDetectedAsync("WIFI:T:WPA;S:HomeNet;P:pass123;;");

        Assert.Equal(ScanState.Paused, vm.ScanState);
    }

    // --- Permission denied state ---

    [Fact]
    public async Task OnAppearing_PermissionDenied_SetsPermissionDeniedState()
    {
        _permissions.IsGranted = false;
        _permissions.RequestResult = false;
        var vm = CreateSut();

        await vm.OnAppearingAsync();

        Assert.Equal(ScanState.PermissionDenied, vm.ScanState);
    }

    [Fact]
    public async Task OnAppearing_PermissionDenied_NavigatesToPermissionDeniedPage()
    {
        _permissions.IsGranted = false;
        _permissions.RequestResult = false;
        var vm = CreateSut();

        await vm.OnAppearingAsync();

        Assert.Equal("//permissiondenied", _navigation.LastRoute);
    }

    [Fact]
    public async Task OnAppearing_PermissionGranted_SetsScanningState()
    {
        _permissions.IsGranted = true;
        var vm = CreateSut();

        await vm.OnAppearingAsync();

        Assert.Equal(ScanState.Scanning, vm.ScanState);
    }

    // --- Scanning pause guard ---

    [Fact]
    public async Task OnQrDetected_WhenPaused_IgnoresDetection()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Paused;

        await vm.OnQrDetectedAsync("WIFI:T:WPA;S:HomeNet;P:pass123;;");

        Assert.Empty(_navigation.History);
    }
}
