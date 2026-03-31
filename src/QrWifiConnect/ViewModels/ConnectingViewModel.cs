using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect.ViewModels;

/// <summary>
/// ViewModel for the connecting (spinner) page.
/// Receives WifiCredential via Shell navigation parameters, initiates the connection,
/// then navigates to the result page.
/// Clears the credential reference immediately after ConnectAsync returns.
/// </summary>
public sealed partial class ConnectingViewModel : ObservableObject, IQueryAttributable
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);

    private readonly IWifiConnector _wifiConnector;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private WifiCredential? _credential;

    public ConnectingViewModel(IWifiConnector wifiConnector, INavigationService navigation)
    {
        _wifiConnector = wifiConnector;
        _navigation = navigation;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("credential", out var value) && value is WifiCredential cred)
            Credential = cred;
    }

    /// <summary>
    /// Called by the page's OnAppearing override.
    /// Initiates the WiFi connection and navigates to the result page.
    /// </summary>
    [RelayCommand]
    public async Task OnAppearingAsync()
    {
        var cred = Credential;
        if (cred is null)
            return;

        ConnectionResult result;

        using var cts = new CancellationTokenSource(ConnectionTimeout);
        try
        {
            result = await _wifiConnector.ConnectAsync(cred, cts.Token);
        }
        catch (OperationCanceledException)
        {
            result = ConnectionResult.CreateFailure(
                cred.Ssid,
                "Connection timed out. The network may be out of range.",
                isTimeout: true);
        }

        await _navigation.GoToAsync("result", new Dictionary<string, object>
        {
            ["connectionResult"] = result
        });
    }
}
