using QrWifiConnect.Models;
using QrWifiConnect.Services;
using QrWifiConnect.Tests.Fakes;
using QrWifiConnect.ViewModels;
using Xunit;

namespace QrWifiConnect.Tests.ViewModels;

public sealed class ScannerCameraUnavailableTests
{
    private readonly QrParserService _parser = new();
    private readonly FakeCameraPermissionService _permissions = new() { IsGranted = true };
    private readonly FakeNavigationService _navigation = new();

    private ScannerViewModel CreateSut() =>
        new(_parser, _permissions, _navigation);

    [Fact]
    public void OnCameraUnavailable_TransitionsToUnavailableState()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        vm.OnCameraUnavailable();

        Assert.Equal(ScanState.CameraUnavailable, vm.ScanState);
    }

    [Fact]
    public void OnCameraUnavailable_SetsIsCameraUnavailableTrue()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.Scanning;

        vm.OnCameraUnavailable();

        Assert.True(vm.IsCameraUnavailable);
        Assert.False(vm.IsCameraEnabled);
    }

    [Fact]
    public void OnCameraAvailable_RecoversToCameraScanning()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.CameraUnavailable;

        vm.OnCameraAvailable();

        Assert.Equal(ScanState.Scanning, vm.ScanState);
    }

    [Fact]
    public void OnCameraAvailable_AfterRecovery_IsCameraEnabledTrue()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.CameraUnavailable;

        vm.OnCameraAvailable();

        Assert.True(vm.IsCameraEnabled);
        Assert.False(vm.IsCameraUnavailable);
    }

    [Fact]
    public async Task WhenCameraUnavailable_QrDetection_IsIgnored()
    {
        var vm = CreateSut();
        vm.ScanState = ScanState.CameraUnavailable;

        await vm.OnQrDetectedAsync("WIFI:T:WPA;S:TestNet;P:pass123;;");

        Assert.Empty(_navigation.History);
    }
}
