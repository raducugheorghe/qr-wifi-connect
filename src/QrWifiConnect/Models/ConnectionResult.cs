namespace QrWifiConnect.Models;

/// <summary>
/// Sealed discriminated union for WiFi connection outcomes.
/// Created by IWifiConnector, consumed by ConnectingViewModel → ResultViewModel.
/// </summary>
public abstract class ConnectionResult
{
    private ConnectionResult() { }

    /// <summary>Creates a successful connection result.</summary>
    public static ConnectionResult CreateSuccess(string ssid) => new SuccessResult(ssid);

    /// <summary>Creates a failure result.</summary>
    public static ConnectionResult CreateFailure(string ssid, string reason, bool isTimeout = false)
        => new FailureResult(ssid, reason, isTimeout);

    /// <summary>Represents a successful WiFi join.</summary>
    public sealed class SuccessResult : ConnectionResult
    {
        internal SuccessResult(string ssid) => Ssid = ssid;

        /// <summary>The SSID the device connected to.</summary>
        public string Ssid { get; }
    }

    /// <summary>Represents a failed WiFi join attempt.</summary>
    public sealed class FailureResult : ConnectionResult
    {
        internal FailureResult(string ssid, string reason, bool isTimeout)
        {
            Ssid = ssid;
            Reason = reason;
            IsTimeout = isTimeout;
        }

        /// <summary>The SSID that was attempted.</summary>
        public string Ssid { get; }

        /// <summary>Plain-language message shown to the user.</summary>
        public string Reason { get; }

        /// <summary>True when the OS timed out; drives the timeout-specific UI message.</summary>
        public bool IsTimeout { get; }
    }
}
