namespace QrWifiConnect.Models;

/// <summary>
/// Represents the current state of the camera scanner,
/// used by ScannerViewModel to drive ScannerPage rendering.
/// </summary>
public enum ScanState
{
    /// <summary>Camera is active and scanning for QR codes.</summary>
    Scanning,

    /// <summary>Camera is active but scanning is paused (QR detected, awaiting navigation).</summary>
    Paused,

    /// <summary>Camera permission has been denied; show guidance UI.</summary>
    PermissionDenied,

    /// <summary>Camera hardware is not accessible (taken by another app or error).</summary>
    CameraUnavailable
}
