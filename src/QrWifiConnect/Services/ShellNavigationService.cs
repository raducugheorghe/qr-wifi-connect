namespace QrWifiConnect.Services;

/// <summary>
/// Production implementation of <see cref="INavigationService"/>
/// that delegates to <see cref="Shell.Current"/> and <see cref="Application.Current"/>.
/// </summary>
internal sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) =>
        Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);

    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");

    public void QuitApplication() =>
        Application.Current?.Quit();
}
