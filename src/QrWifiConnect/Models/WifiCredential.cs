namespace QrWifiConnect.Models;

/// <summary>
/// Represents the decoded, validated contents of a WIFI: QR payload.
/// Invariant: Password must be non-null/non-empty when SecurityType != Open.
/// Privacy invariant: ToString() never exposes the password.
/// </summary>
public sealed record WifiCredential
{
    /// <summary>Network SSID. Non-empty, max 32 UTF-8 characters (IEEE 802.11).</summary>
    public required string Ssid { get; init; }

    /// <summary>Security type parsed from the T: field.</summary>
    public required WifiSecurityType SecurityType { get; init; }

    /// <summary>Network password. Null for Open networks; non-empty otherwise.</summary>
    public string? Password { get; init; }

    /// <summary>True when the QR code encodes H:true (hidden SSID).</summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Returns a safe representation that deliberately omits the password.
    /// This is called by the runtime for logging, debugger display, and any
    /// string interpolation — the password is never leaked via this path.
    /// </summary>
    public override string ToString() =>
        $"WifiCredential(SSID={Ssid}, SecurityType={SecurityType}, IsHidden={IsHidden})";
}
