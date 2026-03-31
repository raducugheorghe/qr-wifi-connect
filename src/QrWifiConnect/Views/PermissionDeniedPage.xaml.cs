using QrWifiConnect.ViewModels;

namespace QrWifiConnect.Views;

public partial class PermissionDeniedPage : ContentPage
{
    private readonly PermissionDeniedViewModel _viewModel;

    public PermissionDeniedPage(PermissionDeniedViewModel viewModel)
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
