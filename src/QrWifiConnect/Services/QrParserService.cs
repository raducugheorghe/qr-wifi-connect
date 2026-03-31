using System.Text.RegularExpressions;
using QrWifiConnect.Models;

namespace QrWifiConnect.Services;

/// <summary>
/// Parses raw QR code values into WiFi credentials using strict regex validation
/// against the WIFI: URI schema (ZXing/Google standard).
/// Format: WIFI:&lt;fields&gt;;; — fields (T, S, P, H) may appear in any order.
/// </summary>
internal sealed partial class QrParserService : IQrParserService
{
    // Strict regex that anchors to the full WIFI: schema.
    // Each field value is captured as literal text — semicolons and special characters
    // within quoted values are extracted verbatim and never interpreted as code.
    // Named groups: type, ssid, password (optional), hidden (optional).
    // Fields may appear in any order; unknown fields are accepted and skipped.
    [GeneratedRegex(
        @"^WIFI:(?:(?:T:(?<type>[^;]*)|S:(?<ssid>[^;]+)|P:(?<password>[^;]*)|H:(?<hidden>true|false)|[^;]*);)*;$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex WifiUriRegex();

    public WifiCredential? TryParse(string rawQrValue)
    {
        if (string.IsNullOrWhiteSpace(rawQrValue))
            return null;

        if (!rawQrValue.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase))
            return null;

        var match = WifiUriRegex().Match(rawQrValue);
        if (!match.Success)
            return null;

        var ssid = match.Groups["ssid"].Value;
        if (string.IsNullOrEmpty(ssid) || ssid.Length > 32)
            return null;

        var securityType = ParseSecurityType(match.Groups["type"].Value);
        var password = match.Groups["password"].Success ? match.Groups["password"].Value : null;
        var isHidden = string.Equals(match.Groups["hidden"].Value, "true", StringComparison.OrdinalIgnoreCase);

        // Enforce password invariant: required for non-open networks
        if (securityType != WifiSecurityType.Open && string.IsNullOrEmpty(password))
            return null;

        // Open networks must not carry a password field
        if (securityType == WifiSecurityType.Open)
            password = null;

        return new WifiCredential
        {
            Ssid = ssid,
            SecurityType = securityType,
            Password = password,
            IsHidden = isHidden
        };
    }

    private static WifiSecurityType ParseSecurityType(string typeToken) =>
        typeToken.ToUpperInvariant() switch
        {
            "WPA"    => WifiSecurityType.Wpa,
            "WPA2"   => WifiSecurityType.Wpa,
            "WPA3"   => WifiSecurityType.Wpa3,
            "WEP"    => WifiSecurityType.Wep,
            "NOPASS" => WifiSecurityType.Open,
            _        => WifiSecurityType.Open  // empty = open network
        };
}
