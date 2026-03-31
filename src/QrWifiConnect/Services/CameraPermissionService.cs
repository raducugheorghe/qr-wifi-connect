namespace QrWifiConnect.Services;

/// <summary>
/// Concrete implementation of <see cref="ICameraPermissionService"/>.
/// Owns camera permission lifecycle: check, request, and system settings navigation.
/// </summary>
internal sealed class CameraPermissionService : ICameraPermissionService
{
    public async Task<bool> IsCameraPermissionGrantedAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        return status == PermissionStatus.Granted;
    }

    public async Task<bool> RequestCameraPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        return status == PermissionStatus.Granted;
    }

    public void OpenSystemSettings()
    {
        // Opens macOS Privacy & Security › Camera settings pane
        _ = Launcher.OpenAsync("x-apple.systempreferences:com.apple.preference.security?Privacy_Camera");
    }
}
