using QrWifiConnect.Views;

namespace QrWifiConnect;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute("confirmation", typeof(ConfirmationPage));
        Routing.RegisterRoute("connecting", typeof(ConnectingPage));
        Routing.RegisterRoute("result", typeof(ResultPage));
        Routing.RegisterRoute("permissiondenied", typeof(PermissionDeniedPage));
    }
}
