using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QrWifiConnect.Services;
using QrWifiConnect.ViewModels;
using QrWifiConnect.Views;
using BarcodeScanning;

namespace QrWifiConnect;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeScanning()
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        // Services — singletons
        builder.Services.AddSingleton<IQrParserService, QrParserService>();
        builder.Services.AddSingleton<ICameraPermissionService, CameraPermissionService>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

#if MACCATALYST
        builder.Services.AddSingleton<IWifiConnector, CoreWlanWifiConnector>();
#endif

        // ViewModels — transient (new instance per page navigation)
        builder.Services.AddTransient<ScannerViewModel>();
        builder.Services.AddTransient<ConfirmationViewModel>();
        builder.Services.AddTransient<ConnectingViewModel>();
        builder.Services.AddTransient<ResultViewModel>();
        builder.Services.AddTransient<PermissionDeniedViewModel>();

        // Pages — transient
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<ConfirmationPage>();
        builder.Services.AddTransient<ConnectingPage>();
        builder.Services.AddTransient<ResultPage>();
        builder.Services.AddTransient<PermissionDeniedPage>();

        return builder.Build();
    }
}
