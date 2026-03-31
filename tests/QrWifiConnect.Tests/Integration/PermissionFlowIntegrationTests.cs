using QrWifiConnect.Models;
using QrWifiConnect.Services;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.Integration;

/// <summary>
/// Integration tests for the permission-denied flow.
/// Uses FakeCameraPermissionService — no hardware dependency.
/// </summary>
public sealed class PermissionFlowIntegrationTests
{
    // --- Permission denied rendering ---

    [Fact]
    public async Task DeniedPermission_ScannerNavigatesToPermissionDeniedPage()
    {
        var parser = new QrParserService();
        var permissions = new FakeCameraPermissionService
        {
            IsGranted = false,
            RequestResult = false
        };
        var navigation = new FakeNavigationService();
        var scanner = new ScannerViewModel(parser, permissions, navigation);

        await scanner.OnAppearingAsync();

        Assert.Equal("//permissiondenied", navigation.LastRoute);
        Assert.Equal(ScanState.PermissionDenied, scanner.ScanState);
    }

    // --- Open System Settings action ---

    [Fact]
    public void OpenSettings_CallsPermissionServiceOpenSystemSettings()
    {
        var permissions = new FakeCameraPermissionService();
        var navigation = new FakeNavigationService();
        var vm = new PermissionDeniedViewModel(permissions, navigation);

        vm.OpenSettingsCommand.Execute(null);

        Assert.Equal(1, permissions.OpenSettingsCallCount);
    }

    // --- Resume-to-scanning after permission grant ---

    [Fact]
    public async Task PermissionDeniedViewModel_WhenGrantedOnResume_NavigatesToScanner()
    {
        var permissions = new FakeCameraPermissionService { IsGranted = true };
        var navigation = new FakeNavigationService();
        var vm = new PermissionDeniedViewModel(permissions, navigation);

        await vm.OnAppearingAsync();

        Assert.Equal("//scanner", navigation.LastRoute);
    }

    [Fact]
    public async Task PermissionDeniedViewModel_WhenStillDeniedOnResume_DoesNotNavigate()
    {
        var permissions = new FakeCameraPermissionService { IsGranted = false };
        var navigation = new FakeNavigationService();
        var vm = new PermissionDeniedViewModel(permissions, navigation);

        await vm.OnAppearingAsync();

        Assert.Empty(navigation.History);
    }

    // --- Scanner auto-resumes after permission grant ---

    [Fact]
    public async Task ScannerViewModel_WhenPermissionGrantedAfterDenied_TransitionsToScanning()
    {
        var parser = new QrParserService();
        var permissions = new FakeCameraPermissionService
        {
            IsGranted = false,
            RequestResult = false
        };
        var navigation = new FakeNavigationService();
        var scanner = new ScannerViewModel(parser, permissions, navigation);

        // First appearing — denied
        await scanner.OnAppearingAsync();
        Assert.Equal(ScanState.PermissionDenied, scanner.ScanState);

        // Permission is now granted (user went to settings)
        permissions.IsGranted = true;

        // Second appearing — simulate app resume back to scanner
        await scanner.OnAppearingAsync();
        Assert.Equal(ScanState.Scanning, scanner.ScanState);
    }
}
