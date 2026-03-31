namespace QrWifiConnect.Models;

/// <summary>
/// Security types supported by the WIFI: QR code standard.
/// Maps to the value of the T: field in the payload.
/// </summary>
public enum WifiSecurityType
{
    /// <summary>WPA Personal / WPA2 Personal (most home/office networks).</summary>
    Wpa,

    /// <summary>WPA3 Personal — some routers emit WPA3 explicitly.</summary>
    Wpa3,

    /// <summary>WEP — legacy; included for completeness.</summary>
    Wep,

    /// <summary>Open network — no password required.</summary>
    Open
}
