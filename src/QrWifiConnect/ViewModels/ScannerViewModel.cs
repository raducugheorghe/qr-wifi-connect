using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect.ViewModels;

/// <summary>
/// ViewModel for the scanner page.
/// Orchestrates camera permission checks, QR detection, and navigation.
/// </summary>
public sealed partial class ScannerViewModel : ObservableObject
{
    private readonly IQrParserService _qrParser;
    private readonly ICameraPermissionService _permissionService;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ScanState _scanState = ScanState.Scanning;

    public ScannerViewModel(
        IQrParserService qrParser,
        ICameraPermissionService permissionService,
        INavigationService navigation)
    {
        _qrParser = qrParser;
        _permissionService = permissionService;
        _navigation = navigation;
    }

    /// <summary>True when the live camera viewfinder should be active.</summary>
    public bool IsCameraEnabled => ScanState == ScanState.Scanning || ScanState == ScanState.Paused;

    /// <summary>True when the camera hardware is unavailable — drives the unavailable UI panel.</summary>
    public bool IsCameraUnavailable => ScanState == ScanState.CameraUnavailable;

    partial void OnScanStateChanged(ScanState value)
    {
        OnPropertyChanged(nameof(IsCameraEnabled));
        OnPropertyChanged(nameof(IsCameraUnavailable));
    }

    /// <summary>
    /// Called by the page's OnAppearing override.
    /// Checks camera permission and transitions state accordingly.
    /// US3: Re-checks on resume — transitions PermissionDenied → Scanning if now granted.
    /// </summary>
    [RelayCommand]
    public async Task OnAppearingAsync()
    {
        var granted = await _permissionService.IsCameraPermissionGrantedAsync();

        if (granted)
        {
            if (ScanState == ScanState.PermissionDenied || ScanState == ScanState.Paused)
            {
                // Restore scanning after permission grant or returning from confirmation
                ScanState = ScanState.Scanning;
            }
            else if (ScanState != ScanState.CameraUnavailable)
            {
                ScanState = ScanState.Scanning;
            }
        }
        else
        {
            // Request permission on first launch
            var requested = await _permissionService.RequestCameraPermissionAsync();
            ScanState = requested ? ScanState.Scanning : ScanState.PermissionDenied;

            if (ScanState == ScanState.PermissionDenied)
                await _navigation.GoToAsync("permissiondenied");
        }
    }

    /// <summary>
    /// Called by the page when a QR barcode is detected.
    /// Non-WiFi codes and invalid payloads are silently ignored.
    /// </summary>
    public async Task OnQrDetectedAsync(string rawValue)
    {
        if (ScanState != ScanState.Scanning)
            return;

        var credential = _qrParser.TryParse(rawValue);
        if (credential is null)
            return;  // Not a WiFi QR or invalid payload — keep scanning

        // Pause scanning to prevent re-triggering during navigation
        ScanState = ScanState.Paused;

        await _navigation.GoToAsync("confirmation", new Dictionary<string, object>
        {
            ["credential"] = credential
        });
    }

    /// <summary>
    /// Called when the camera hardware reports it is unavailable.
    /// </summary>
    public void OnCameraUnavailable()
    {
        ScanState = ScanState.CameraUnavailable;
    }

    /// <summary>
    /// Called when the camera hardware becomes available again after being unavailable.
    /// Falls through the normal appearing logic to re-check permission.
    /// </summary>
    public void OnCameraAvailable()
    {
        if (ScanState == ScanState.CameraUnavailable)
            ScanState = ScanState.Scanning;
    }
}
