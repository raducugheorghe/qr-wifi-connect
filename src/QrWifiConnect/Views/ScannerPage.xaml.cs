using BarcodeScanning;
using System.Linq;
using QrWifiConnect.ViewModels;

namespace QrWifiConnect.Views;

public partial class ScannerPage : ContentPage
{
    private readonly ScannerViewModel _viewModel;

    public ScannerPage(ScannerViewModel viewModel)
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

    /// <summary>
    /// Forwards barcode detection events to the ViewModel.
    /// Does not call permission APIs or perform any business logic directly.
    /// </summary>
    private async void OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        if (e.BarcodeResults is null || e.BarcodeResults.Count == 0)
            return;

        var rawValue = e.BarcodeResults.FirstOrDefault()?.DisplayValue;
        if (string.IsNullOrEmpty(rawValue))
            return;

        await _viewModel.OnQrDetectedAsync(rawValue);
    }
}
