using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect.Tests.Fakes;

/// <summary>
/// Configurable fake WiFi connector for unit and integration tests.
/// No hardware or network dependency.
/// </summary>
public sealed class FakeWifiConnector : IWifiConnector
{
    private ConnectionResult? _result;

    /// <summary>Configures the fake to return a successful connection.</summary>
    public void SetSuccess(string ssid) =>
        _result = ConnectionResult.CreateSuccess(ssid);

    /// <summary>Configures the fake to return a failure.</summary>
    public void SetFailure(string ssid, string reason, bool isTimeout = false) =>
        _result = ConnectionResult.CreateFailure(ssid, reason, isTimeout);

    public Task<ConnectionResult> ConnectAsync(WifiCredential credential, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = _result ?? ConnectionResult.CreateFailure(
            credential.Ssid,
            "FakeWifiConnector: no result configured.");

        return Task.FromResult(result);
    }
}
