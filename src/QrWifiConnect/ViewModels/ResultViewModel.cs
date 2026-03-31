using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect.ViewModels;

/// <summary>
/// ViewModel for the result page.
/// Receives ConnectionResult and drives the success/failure UI states.
/// </summary>
[QueryProperty(nameof(ConnectionResult), "connectionResult")]
public sealed partial class ResultViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSuccess))]
    [NotifyPropertyChangedFor(nameof(Ssid))]
    [NotifyPropertyChangedFor(nameof(Reason))]
    [NotifyPropertyChangedFor(nameof(IsTimeout))]
    private ConnectionResult? _connectionResult;

    public ResultViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public bool IsSuccess => ConnectionResult is ConnectionResult.SuccessResult;

    public string Ssid => ConnectionResult switch
    {
        ConnectionResult.SuccessResult s => s.Ssid,
        ConnectionResult.FailureResult f => f.Ssid,
        _ => string.Empty
    };

    public string Reason => ConnectionResult is ConnectionResult.FailureResult f
        ? f.Reason
        : string.Empty;

    public bool IsTimeout => ConnectionResult is ConnectionResult.FailureResult { IsTimeout: true };

    [RelayCommand]
    private async Task RetryAsync()
    {
        ConnectionResult = null;
        await _navigation.GoToAsync("//scanner");
    }

    [RelayCommand]
    private void Exit()
    {
        ConnectionResult = null;
        _navigation.QuitApplication();
    }
}
