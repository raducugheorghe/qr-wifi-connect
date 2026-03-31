using QrWifiConnect.Models;
using QrWifiConnect.Services;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.Integration;

/// <summary>
/// Integration tests for the scanner detection → confirmation navigation flow.
/// Uses fake camera input and fake services — no real camera hardware required.
/// </summary>
public sealed class ScannerFlowIntegrationTests
{
    private readonly QrParserService _parser = new();
    private readonly FakeCameraPermissionService _permissions = new() { IsGranted = true };
    private readonly FakeNavigationService _navigation = new();

    [Fact]
    public async Task ValidWifiQrDetection_NavigatesToConfirmationWithCredential()
    {
        var scanner = new ScannerViewModel(_parser, _permissions, _navigation);
        await scanner.OnAppearingAsync();

        // Simulate scanner detecting a WiFi QR code
        await scanner.OnQrDetectedAsync("WIFI:T:WPA;S:CafeWifi;P:coffee99;;");

        Assert.Equal("confirmation", _navigation.LastRoute);
        var credential = _navigation.LastParams?["credential"] as WifiCredential;
        Assert.NotNull(credential);
        Assert.Equal("CafeWifi", credential.Ssid);
    }

    [Fact]
    public async Task NonWifiQrDetection_IsIgnored_ScanningContinues()
    {
        var scanner = new ScannerViewModel(_parser, _permissions, _navigation);
        await scanner.OnAppearingAsync();

        // Simulate detecting a URL QR code
        await scanner.OnQrDetectedAsync("https://example.com");

        Assert.Empty(_navigation.History);
        Assert.Equal(ScanState.Scanning, scanner.ScanState);
    }

    [Fact]
    public async Task MultipleMixedQrDetections_OnlyWifiNavigates()
    {
        var scanner = new ScannerViewModel(_parser, _permissions, _navigation);
        await scanner.OnAppearingAsync();

        // First detection: non-WiFi — should be ignored
        await scanner.OnQrDetectedAsync("https://example.com");
        Assert.Empty(_navigation.History);

        // Second detection: WiFi — should navigate
        await scanner.OnQrDetectedAsync("WIFI:T:WPA;S:HomeNet;P:pass123;;");
        Assert.Equal("confirmation", _navigation.LastRoute);
    }

    [Fact]
    public async Task ConfirmationViewModel_ConnectCommand_NavigatesToConnecting()
    {
        var scanner = new ScannerViewModel(_parser, _permissions, _navigation);
        await scanner.OnAppearingAsync();
        await scanner.OnQrDetectedAsync("WIFI:T:WPA;S:HomeNet;P:pass123;;");

        // Confirmation stage
        var credential = _navigation.LastParams?["credential"] as WifiCredential;
        var confirmationNav = new FakeNavigationService();
        var confirmation = new ConfirmationViewModel(confirmationNav)
        {
            Credential = credential
        };

        await confirmation.ConnectCommand.ExecuteAsync(null);

        Assert.Equal("connecting", confirmationNav.LastRoute);
    }
}
