namespace QrWifiConnect.Services;

/// <summary>
/// Abstracts Shell navigation and application lifecycle operations.
/// Injected into ViewModels so they can be tested without MAUI Shell.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to the specified Shell route.</summary>
    Task GoToAsync(string route);

    /// <summary>Navigates to the specified Shell route, passing query parameters.</summary>
    Task GoToAsync(string route, IDictionary<string, object> parameters);

    /// <summary>Navigates one level back in the Shell navigation stack.</summary>
    Task GoBackAsync();

    /// <summary>Terminates the application.</summary>
    void QuitApplication();
}
