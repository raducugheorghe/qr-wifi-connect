using QrWifiConnect.Services;

namespace QrWifiConnect.Tests.Fakes;

/// <summary>
/// Configurable fake camera permission service for unit and integration tests.
/// </summary>
public sealed class FakeCameraPermissionService : ICameraPermissionService
{
    public bool IsGranted { get; set; } = true;
    public bool RequestResult { get; set; } = true;
    public int OpenSettingsCallCount { get; private set; }

    public Task<bool> IsCameraPermissionGrantedAsync() =>
        Task.FromResult(IsGranted);

    public Task<bool> RequestCameraPermissionAsync() =>
        Task.FromResult(RequestResult);

    public void OpenSystemSettings() =>
        OpenSettingsCallCount++;
}
