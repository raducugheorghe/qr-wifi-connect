using QrWifiConnect.ViewModels;

namespace QrWifiConnect.Views;

public partial class ConfirmationPage : ContentPage
{
    public ConfirmationPage(ConfirmationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
