using QrWifiConnect.ViewModels;

namespace QrWifiConnect.Views;

public partial class ResultPage : ContentPage
{
    private readonly ResultViewModel _viewModel;

    public ResultPage(ResultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyVisualState();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResultViewModel.IsSuccess))
            ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        var stateName = _viewModel.IsSuccess ? "Success" : "Failure";
        VisualStateManager.GoToState(RootLayout, stateName);
    }
}
