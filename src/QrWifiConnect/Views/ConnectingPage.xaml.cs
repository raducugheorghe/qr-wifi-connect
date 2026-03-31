using QrWifiConnect.ViewModels;

namespace QrWifiConnect.Views;

public partial class ConnectingPage : ContentPage
{
    private readonly ConnectingViewModel _viewModel;

    public ConnectingPage(ConnectingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
