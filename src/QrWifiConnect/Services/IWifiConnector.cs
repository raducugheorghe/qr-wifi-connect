using QrWifiConnect.Models;

namespace QrWifiConnect.Services;

/// <summary>
/// Initiates a WiFi network join via platform APIs.
/// Platform implementation: Platforms/MacCatalyst/CoreWlanWifiConnector.cs
/// </summary>
public interface IWifiConnector
{
    /// <summary>
    /// Attempts to join the specified WiFi network.
    /// </summary>
    /// <param name="credential">The network credentials parsed from the QR code.</param>
    /// <param name="ct">Cancellation token; the implementation uses a 30-second timeout.</param>
    /// <returns>A <see cref="ConnectionResult"/> indicating success or failure.</returns>
    Task<ConnectionResult> ConnectAsync(WifiCredential credential, CancellationToken ct = default);
}
