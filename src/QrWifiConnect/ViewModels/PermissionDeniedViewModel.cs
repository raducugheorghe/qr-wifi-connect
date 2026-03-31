using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QrWifiConnect.Services;

namespace QrWifiConnect.ViewModels;

/// <summary>
/// ViewModel for the permission-denied page.
/// Shows guidance and provides a button to open System Settings.
/// On app resume, re-checks permission — navigates back to scanner if now granted.
/// </summary>
public sealed partial class PermissionDeniedViewModel : ObservableObject
{
    private readonly ICameraPermissionService _permissionService;
    private readonly INavigationService _navigation;

    public PermissionDeniedViewModel(
        ICameraPermissionService permissionService,
        INavigationService navigation)
    {
        _permissionService = permissionService;
        _navigation = navigation;
    }

    /// <summary>
    /// Called by the page's OnAppearing override (e.g., app resume after Settings visit).
    /// Automatically transitions back to scanner if camera permission is now granted.
    /// </summary>
    [RelayCommand]
    public async Task OnAppearingAsync()
    {
        var granted = await _permissionService.IsCameraPermissionGrantedAsync();
        if (granted)
            await _navigation.GoToAsync("//scanner");
    }

    /// <summary>
    /// Opens the macOS Privacy &amp; Security → Camera system settings pane.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _permissionService.OpenSystemSettings();
    }
}
