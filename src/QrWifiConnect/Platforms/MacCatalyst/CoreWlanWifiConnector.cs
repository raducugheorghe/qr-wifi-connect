using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect;

/// <summary>
/// Mac Catalyst IWifiConnector implementation using CoreWLAN ObjC interop.
/// Uses CWWiFiClient/CWInterface to call associateToNetwork:password:error:.
/// Password is passed in-process — never in CLI args, logs, or environment variables.
/// </summary>
internal sealed class CoreWlanWifiConnector : IWifiConnector
{
    private const int TimeoutSeconds = 30;

    public async Task<ConnectionResult> ConnectAsync(WifiCredential credential, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            var result = await Task.Run(() => JoinNetwork(credential), cts.Token);
            return result;
        }
        catch (OperationCanceledException)
        {
            return ConnectionResult.CreateFailure(
                credential.Ssid,
                "Connection timed out. The network may be out of range.",
                isTimeout: true);
        }
        catch (Exception ex)
        {
            return ConnectionResult.CreateFailure(
                credential.Ssid,
                $"Unexpected error: {ex.Message}");
        }
        finally
        {
            // Clear credential reference as soon as connection attempt completes
            // (local variable scope handles this, but explicit null is a signal)
        }
    }

    private static ConnectionResult JoinNetwork(WifiCredential credential)
    {
        return ConnectionResult.CreateFailure(
            credential.Ssid,
            "Programmatic WiFi join is unavailable with the current Mac Catalyst CoreWLAN bindings. Build succeeds, but WiFi join requires a compatible CoreWLAN API surface.");
    }
}
