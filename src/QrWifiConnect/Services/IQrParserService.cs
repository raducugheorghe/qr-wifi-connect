using QrWifiConnect.Models;

namespace QrWifiConnect.Services;

/// <summary>
/// Parses raw QR code values into WiFi credentials.
/// </summary>
public interface IQrParserService
{
    /// <summary>
    /// Attempts to parse a WIFI: URI from a raw QR code string.
    /// </summary>
    /// <param name="rawQrValue">The raw string value detected by the barcode scanner.</param>
    /// <returns>
    /// A <see cref="WifiCredential"/> when the value is a valid WIFI: URI;
    /// <c>null</c> for non-WIFI: codes or malformed payloads.
    /// </returns>
    WifiCredential? TryParse(string rawQrValue);
}
