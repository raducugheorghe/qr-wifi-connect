using QrWifiConnect.Services;

namespace QrWifiConnect.Tests.Fakes;

/// <summary>
/// Fake navigation service that records navigation calls for test assertions.
/// </summary>
public sealed class FakeNavigationService : INavigationService
{
    private readonly List<(string Route, IDictionary<string, object>? Params)> _history = [];

    public IReadOnlyList<(string Route, IDictionary<string, object>? Params)> History => _history;

    public string? LastRoute => _history.Count > 0 ? _history[^1].Route : null;
    public IDictionary<string, object>? LastParams => _history.Count > 0 ? _history[^1].Params : null;

    public int QuitCallCount { get; private set; }

    public Task GoToAsync(string route)
    {
        _history.Add((route, null));
        return Task.CompletedTask;
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        _history.Add((route, parameters));
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        _history.Add(("..", null));
        return Task.CompletedTask;
    }

    public void QuitApplication()
    {
        QuitCallCount++;
    }
}
