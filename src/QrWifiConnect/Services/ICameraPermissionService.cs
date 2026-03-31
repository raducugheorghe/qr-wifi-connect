namespace QrWifiConnect.Services;

/// <summary>
/// Owns camera permission checks, requests, and system settings navigation.
/// The single permission abstraction consumed by ScannerViewModel.
/// </summary>
public interface ICameraPermissionService
{
    /// <summary>Returns true when camera permission is currently granted.</summary>
    Task<bool> IsCameraPermissionGrantedAsync();

    /// <summary>
    /// Requests camera permission from the OS.
    /// </summary>
    /// <returns>True if the user granted permission; false if denied.</returns>
    Task<bool> RequestCameraPermissionAsync();

    /// <summary>
    /// Opens the macOS Privacy &amp; Security → Camera system settings pane.
    /// </summary>
    void OpenSystemSettings();
}
