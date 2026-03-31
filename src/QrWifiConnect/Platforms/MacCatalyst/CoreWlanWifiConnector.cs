using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Foundation;
using ObjCRuntime;
using QrWifiConnect.Models;
using QrWifiConnect.Services;

namespace QrWifiConnect;

/// <summary>
/// Mac Catalyst IWifiConnector implementation.
///
/// Scan : CoreWLAN ObjC interop (CWWiFiClient/CWInterface) — verifies the SSID is in range
///        before attempting a join.
/// Join : /usr/sbin/networksetup -setairportnetwork — the only non-root, non-sandboxed path
///        that reliably works on Mac Catalyst.
///
///   WHY NOT CWInterface.associateToNetwork:password:error:?
///     Returns tmpErr (-32767) on Mac Catalyst because airportd rejects XPC association
///     requests from non-native processes on macOS 13+.
///
///   WHY NOT NEHotspotConfigurationManager?
///     Compiles on Mac Catalyst but always returns NEHotspotConfigurationError.Internal (8)
///     because the iOS neagent WiFi-config daemon does not exist on macOS.
///
/// Requires app-sandbox = false in Entitlements.plist (already set).
///
/// SECURITY NOTE — password in process args:
///   networksetup receives the password as a CLI argument. On macOS, process arguments are
///   momentarily visible to same-UID processes via ps(1) or the kernel proc table. This is
///   an inherent limitation of the networksetup API; there is no stdin/pipe interface.
///   Risk is bounded: only same-user processes can observe it, and the arg is cleared as
///   soon as networksetup exits (typically &lt;2 s). No password ever appears in logs or stdout.
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
            return await JoinNetworkAsync(credential, cts.Token);
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
    }

    // ── objc_msgSend P/Invoke overloads (CoreWLAN scan only) ──────────────────

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_msgSend_IntPtr_refIntPtr(
        IntPtr receiver, IntPtr selector, IntPtr arg1, ref IntPtr arg2);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern nuint nuint_msgSend(IntPtr receiver, IntPtr selector);

    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<ConnectionResult> JoinNetworkAsync(WifiCredential credential, CancellationToken ct)
    {
        // Phase 1: CoreWLAN scan — verify the SSID is reachable before attempting a join.
        string? scanError = await Task.Run(() => TryScanForNetwork(credential.Ssid), ct);
        if (scanError is not null)
            return ConnectionResult.CreateFailure(credential.Ssid, scanError);

        // Phase 2: join via networksetup (off the UI thread to avoid blocking it).
        return await Task.Run(() => JoinViaNetworkSetup(credential), ct);
    }

    // ── Phase 1: CoreWLAN scan ─────────────────────────────────────────────────

    /// <returns>null when the SSID was found; a diagnostic error string otherwise.</returns>
    private static string? TryScanForNetwork(string ssid)
    {
        Dlfcn.dlopen("/System/Library/Frameworks/CoreWLAN.framework/CoreWLAN", 0);

        var cwWifiClientClass = Class.GetHandle("CWWiFiClient");
        if (cwWifiClientClass == IntPtr.Zero)
            return "CoreWLAN framework is not available on this system.";

        var sharedClient = IntPtr_msgSend(cwWifiClientClass, Selector.GetHandle("sharedWiFiClient"));
        if (sharedClient == IntPtr.Zero)
            return "Could not obtain CWWiFiClient shared instance.";

        var cwInterface = IntPtr_msgSend(sharedClient, Selector.GetHandle("interface"));
        if (cwInterface == IntPtr.Zero)
            return "No Wi-Fi interface found. Ensure Wi-Fi is enabled.";

        using var ssidNs = new NSString(ssid);
        IntPtr scanError = IntPtr.Zero;
        var networksSet = IntPtr_msgSend_IntPtr_refIntPtr(
            cwInterface, Selector.GetHandle("scanForNetworksWithName:error:"),
            ssidNs.Handle, ref scanError);

        if (scanError != IntPtr.Zero)
        {
            var err = Runtime.GetNSObject<NSError>(scanError)!;
            return $"Scan failed:\n{FormatNSError("CWInterface.scanForNetworksWithName:error:", err)}";
        }

        if (networksSet == IntPtr.Zero || nuint_msgSend(networksSet, Selector.GetHandle("count")) == 0)
            return $"Network \"{ssid}\" was not found nearby.";

        return null;
    }

    // ── Phase 2: networksetup join ─────────────────────────────────────────────

    private static ConnectionResult JoinViaNetworkSetup(WifiCredential credential)
    {
        string device = FindWifiDevice() ?? "en0";

        // Build argument list. ArgumentList passes each entry as a discrete argv element,
        // avoiding shell injection. The password arg is cleared when networksetup exits.
        var psi = new ProcessStartInfo("/usr/sbin/networksetup")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("-setairportnetwork");
        psi.ArgumentList.Add(device);
        psi.ArgumentList.Add(credential.Ssid);
        if (credential.Password is { Length: > 0 } pwd)
            psi.ArgumentList.Add(pwd);

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return ConnectionResult.CreateFailure(credential.Ssid,
                $"Failed to launch networksetup: {ex.Message}");
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
            return ConnectionResult.CreateSuccess(credential.Ssid);

        // networksetup writes errors to stdout (not stderr) on some macOS versions.
        string output = string.Join(" | ", new[] { stdout.Trim(), stderr.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return ConnectionResult.CreateFailure(credential.Ssid,
            $"networksetup -setairportnetwork failed (exit {process.ExitCode}, device \"{device}\"):\n{output}");
    }

    /// <summary>
    /// Parses <c>networksetup -listallhardwareports</c> to find the BSD device name
    /// (e.g. "en0") for the Wi-Fi hardware port.
    /// Returns null if the name cannot be determined; caller falls back to "en0".
    /// </summary>
    private static string? FindWifiDevice()
    {
        try
        {
            var psi = new ProcessStartInfo("/usr/sbin/networksetup")
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
            };
            psi.ArgumentList.Add("-listallhardwareports");

            using var p = new Process { StartInfo = psi };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            // Each block looks like:
            //   Hardware Port: Wi-Fi
            //   Device: en0
            //   Ethernet Address: ...
            // Capture the Device: line immediately after a Wi-Fi/AirPort port.
            bool nextIsDevice = false;
            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (nextIsDevice)
                {
                    if (trimmed.StartsWith("Device:", StringComparison.OrdinalIgnoreCase))
                        return trimmed["Device:".Length..].Trim();
                    nextIsDevice = false; // unexpected format; reset
                }

                if (trimmed.StartsWith("Hardware Port:", StringComparison.OrdinalIgnoreCase))
                {
                    var port = trimmed["Hardware Port:".Length..].Trim();
                    if (port.Equals("Wi-Fi", StringComparison.OrdinalIgnoreCase)
                        || port.Equals("AirPort", StringComparison.OrdinalIgnoreCase))
                        nextIsDevice = true;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // ── Error diagnostics ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds a detailed diagnostic string from an NSError: domain, code, localized strings,
    /// every UserInfo entry, and the full underlying error chain.
    /// </summary>
    private static string FormatNSError(string context, NSError error, int depth = 0)
    {
        var pad = new string(' ', depth * 2);
        var sb  = new StringBuilder();

        sb.AppendLine($"{pad}[{context}]");
        sb.AppendLine($"{pad}  Description : {error.LocalizedDescription}");
        sb.AppendLine($"{pad}  Domain      : {error.Domain}");
        sb.AppendLine($"{pad}  Code        : {error.Code}");

        if (!string.IsNullOrWhiteSpace(error.LocalizedFailureReason))
            sb.AppendLine($"{pad}  Reason      : {error.LocalizedFailureReason}");

        if (!string.IsNullOrWhiteSpace(error.LocalizedRecoverySuggestion))
            sb.AppendLine($"{pad}  Recovery    : {error.LocalizedRecoverySuggestion}");

        if (error.UserInfo is { Count: > 0 } info)
        {
            sb.AppendLine($"{pad}  UserInfo:");
            foreach (NSObject key in info.Keys)
            {
                var val = info[key];
                if (val is NSError nested)
                {
                    sb.AppendLine($"{pad}    {key}:");
                    sb.AppendLine(FormatNSError(key.Description, nested, depth + 3));
                }
                else
                {
                    sb.AppendLine($"{pad}    {key} = {val}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
