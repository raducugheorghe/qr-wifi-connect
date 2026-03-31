using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect.ViewModels;

/// <summary>
/// ViewModel for the confirmation page.
/// Receives WifiCredential via Shell navigation parameters, shows SSID/security info,
/// and lets the user confirm or cancel the connection.
/// </summary>
public sealed partial class ConfirmationViewModel : ObservableObject, IQueryAttributable
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Ssid))]
    [NotifyPropertyChangedFor(nameof(SecurityType))]
    [NotifyPropertyChangedFor(nameof(IsHidden))]
    private WifiCredential? _credential;

    public ConfirmationViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("credential", out var value) && value is WifiCredential cred)
            Credential = cred;
    }

    public string Ssid => Credential?.Ssid ?? string.Empty;
    public WifiSecurityType? SecurityType => Credential?.SecurityType;
    public bool IsHidden => Credential?.IsHidden ?? false;

    [RelayCommand]
    private async Task ConnectAsync()
    {
        var cred = Credential;
        if (cred is null)
            return;
        
        await _navigation.GoToAsync("connecting", new Dictionary<string, object>
        {
            ["credential"] = cred
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        Credential = null;
        await _navigation.GoToAsync("//scanner");
    }
}
