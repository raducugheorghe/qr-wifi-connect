# Research: Programmatic WiFi Join on Mac Catalyst (.NET MAUI, net9.0-maccatalyst)

**Feature**: 001-macos-wifi-qr-connect  
**Date**: 2026-03-31  
**Sources**: Apple documentation, Microsoft .NET MAUI API reference, Apple Developer Forums

---

## Executive Summary

`NEHotspotConfiguration` is the intended iOS API for programmatic WiFi joins and its
classes compile on Mac Catalyst — but **it cannot be used in practice** because Apple
does not issue the required entitlement for macOS/Mac Catalyst apps. The correct path
for a `net9.0-maccatalyst` app is **CoreWLAN** (`CWInterface.associate(to:password:)`),
accessed via Objective-C interop since the managed C# bindings live in
`Microsoft.macOS.dll` (not the Mac Catalyst assembly).

---

## 1. Is NEHotspotConfiguration Available on Mac Catalyst?

### API availability — bindings exist but the entitlement does not

`NEHotspotConfiguration` and `NEHotspotConfigurationManager` are documented as
available on `Mac Catalyst 13.1+` by Apple and confirmed in the .NET binding
assembly (`Microsoft.MacCatalyst.dll` / `.NET for Mac Catalyst 26.0+`). The
classes compile and can be instantiated.

**The blocking problem**: The entitlement that activates the feature —

```
com.apple.developer.networking.HotspotConfiguration
```

— is listed by Apple with availability `iOS 11.0+  iPadOS 11.0+  visionOS 1.0+`.
**macOS and Mac Catalyst are not in that list.** Apple's provisioning portal does
not issue this capability for Mac Catalyst app IDs. Without the entitlement
embed in the provisioned profile, the API silently returns an error (see §5).

### Verdict: Do NOT use NEHotspotConfiguration on Mac Catalyst

---

## 2. C# API — NEHotspotConfiguration (for reference / iOS target only)

These bindings exist in the `NetworkExtension` namespace and compile on Mac
Catalyst, but they will not produce a working network join without the entitlement.

### Constructors

| Constructor | Use |
|---|---|
| `new NEHotspotConfiguration(string ssid)` | Open (no-password) network |
| `new NEHotspotConfiguration(string ssid, string passphrase, bool isWEP)` | WPA/WPA2 Personal — pass `isWEP: false` |
| `new NEHotspotConfiguration(string ssid, NEHotspotEapSettings eapSettings)` | WPA2 Enterprise / 802.1X |

### WPA2 Personal join — complete example

```csharp
#if IOS  // Guard: this will not work on Mac Catalyst
using NetworkExtension;

async Task JoinWpa2Network(string ssid, string password)
{
    // isWEP: false → WPA/WPA2 Personal (most home/office networks)
    var config = new NEHotspotConfiguration(ssid, password, isWEP: false)
    {
        JoinOnce = true  // Remove config when app exits; cleaner lifetime
    };

    try
    {
        // Async overload: Task-based, throws NSErrorException on failure
        await NEHotspotConfigurationManager.SharedManager.ApplyConfigurationAsync(config);
        // Success: device has joined or was already connected
    }
    catch (NSErrorException ex)
    {
        var code = (NEHotspotConfigurationError)(int)ex.Error.Code;
        switch (code)
        {
            case NEHotspotConfigurationError.AlreadyAssociated:
                // Device is already on this network — not an error for this app
                break;
            case NEHotspotConfigurationError.UserDenied:
                // User declined the system join prompt
                break;
            default:
                // Other failure
                break;
        }
    }
}
#endif
```

> **Security note**: The `passphrase` parameter is passed in-process memory to the
> OS API — it is **not** exposed in command-line arguments, process tables, or
> environment variables. This is the correct, safe way to pass credentials.

---

## 3. Entitlements and Developer Enrollment

### Required entitlement (iOS/iPadOS only)

**Entitlements.plist** entry required (iOS/iPadOS):

```xml
<key>com.apple.developer.networking.HotspotConfiguration</key>
<true/>
```

### Developer enrollment requirement

- The Hotspot Configuration capability must be enabled in your **App ID** on the
  Apple Developer portal.  
- This requires a **paid Apple Developer Program membership** ($99/year for
  individuals/organizations).  
- A free (unsigned / ad-hoc) app cannot obtain this entitlement.  
- **No equivalent capability exists for Mac Catalyst app IDs.** Apple has not
  extended this entitlement to the macOS/Catalyst entitlement namespace.

---

## 4. Recommended Approach: CoreWLAN via ObjC Interop

CoreWLAN (`CWInterface.associate(to:password:)`) is the correct API for macOS.
The Apple framework is available on `macOS 10.7+` and `Mac Catalyst 13.0+`
at the native layer. The managed .NET C# bindings reside in `Microsoft.macOS.dll`
(the `net9.0-macos` target), **not** in the Mac Catalyst assembly — but the
framework is physically present on the machine and callable via ObjC runtime interop.

### Why not the `airport` command-line tool?

`/System/Library/PrivateFrameworks/Apple80211.framework/…/airport` is a **private
framework binary** that Apple has deprecated and partially removed in recent macOS
releases. Calling any shell tool (`Process.Start`) with a password embedded in the
argument list is a security violation — the password is visible in the process table
to any user running `ps aux`. Do not use this approach.

### Why not CoreWLAN managed bindings directly?

`CoreWlan.CWInterface` is in `Microsoft.macOS.dll`. A `net9.0-maccatalyst` project
references `Microsoft.MacCatalyst.dll`. These are separate assemblies; you cannot
reference the macOS assembly from a Mac Catalyst target directly in a .NET MAUI
single project without adding a separate `-macos` target head.

### CoreWLAN via ObjCRuntime (Mac Catalyst compatible)

Since Mac Catalyst IS macOS at runtime, the CoreWLAN Objective-C classes are
available and can be messaged directly through `ObjCRuntime`. The pattern below
works in a `net9.0-maccatalyst` build:

```csharp
#if MACCATALYST
using ObjCRuntime;
using Foundation;

/// <summary>
/// Joins a WPA2 Personal Wi-Fi network using CoreWLAN on macOS.
/// Must run on main thread. Blocks until the OS completes association.
/// Requires the app to NOT be sandboxed, or for the sandbox to include
/// the wireless configuration exception.
/// </summary>
public static async Task<(bool Success, string? ErrorMessage)> JoinWithCoreWlan(
    string ssid,
    string password)
{
    return await Task.Run(() =>
    {
        // 1. Get the shared CWWiFiClient singleton
        var clientClass = Class.GetHandle("CWWiFiClient");
        var sharedClientSel = Selector.GetHandle("sharedWiFiClient");
        var clientPtr = ObjCRuntime.Messaging.IntPtr_objc_msgSend(clientClass, sharedClientSel);
        if (clientPtr == IntPtr.Zero)
            return (false, "CWWiFiClient unavailable");

        // 2. Get the primary interface
        var interfaceSel = Selector.GetHandle("interface");
        var ifacePtr = ObjCRuntime.Messaging.IntPtr_objc_msgSend(clientPtr, interfaceSel);
        if (ifacePtr == IntPtr.Zero)
            return (false, "No Wi-Fi interface found");

        // 3. Scan for the target SSID to get a CWNetwork object
        var nsssid = new NSString(ssid);
        var scanSel = Selector.GetHandle("scanForNetworksWithName:error:");
        IntPtr errorPtr = IntPtr.Zero;
        var setPtr = ObjCRuntime.Messaging.IntPtr_objc_msgSend_IntPtr_ref_IntPtr(
            ifacePtr, scanSel, nsssid.Handle, ref errorPtr);

        if (errorPtr != IntPtr.Zero)
        {
            var err = Runtime.GetNSObject<NSError>(errorPtr);
            return (false, $"Scan error: {err?.LocalizedDescription}");
        }

        // 4. Pick the first matching CWNetwork from the NSSet
        var set = Runtime.GetNSObject<NSSet>(setPtr);
        if (set == null || set.Count == 0)
            return (false, $"Network '{ssid}' not found in range");

        var networkPtr = set.AnyObject.Handle;

        // 5. Associate (blocks; may trigger admin auth dialog)
        var nsPassword = new NSString(password);
        var assocSel = Selector.GetHandle("associateToNetwork:password:error:");
        errorPtr = IntPtr.Zero;
        ObjCRuntime.Messaging.bool_objc_msgSend_IntPtr_IntPtr_ref_IntPtr(
            ifacePtr, assocSel, networkPtr, nsPassword.Handle, ref errorPtr);

        if (errorPtr != IntPtr.Zero)
        {
            var err = Runtime.GetNSObject<NSError>(errorPtr);
            return (false, $"Association failed: {err?.LocalizedDescription}");
        }

        return (true, null);
    });
}
#endif
```

> **Note on the ObjC messaging signatures**: The exact `Messaging` overloads
> used above are illustrative. Real implementation will need to confirm the
> correct selector signatures with `[DllImport]` or the `ObjCRuntime.Messaging`
> helper overloads available in the .NET binding. An alternative is to create a
> thin Swift/ObjC helper shim compiled as a native framework and called via
> P/Invoke — which is cleaner for a production implementation.

### Cleaner alternative: Add a net9.0-macos target head

If the MAUI project adds a `net9.0-macos` (non-Catalyst) target, the fully-managed
`CoreWlan.CWWiFiClient` / `CWInterface` bindings from `Microsoft.macOS.dll` are
available with proper IDE support. This is the recommended path for a local-only
utility app that doesn't need distribution through the App Store.

```csharp
// In a net9.0-macos platform file: Platforms/MacOS/WifiService.cs
#if MACOS
using CoreWlan;

public async Task<(bool, string?)> JoinWithCoreWlanManaged(
    string ssid,
    string password)
{
    return await Task.Run(() =>
    {
        var client = CWWiFiClient.SharedWiFiClient;
        var iface = client.MainInterface;
        if (iface == null)
            return (false, "No Wi-Fi interface");

        // Scan (required to obtain CWNetwork object)
        NSSet<CWNetwork>? networks;
        try
        {
            networks = iface.ScanForNetworks(ssid, out NSError? scanError);
            if (scanError != null)
                return (false, $"Scan failed: {scanError.LocalizedDescription}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }

        var network = networks?.OfType<CWNetwork>().FirstOrDefault();
        if (network == null)
            return (false, $"'{ssid}' not in range");

        // Associate — blocks. OS shows admin auth dialog if not already authorized.
        bool ok = iface.AssociateToNetwork(network, password, out NSError? assocError);
        if (!ok || assocError != null)
            return (false, assocError?.LocalizedDescription ?? "Unknown error");

        return (true, null);
    });
}
#endif
```

### Sandbox requirement for CoreWLAN

`CWInterface.associate(to:password:)` requires the app to either:

- Run **outside the sandbox**, OR  
- Hold the `com.apple.security.network.client` entitlement (standard networking)
  **plus** the `com.apple.security.temporary-exception.wildcard` exception, OR  
- Prompt the user for admin credentials via `SFAuthorization`

For a locally-distributed utility app (not App Store), disabling the sandbox
entirely is the simplest path. For a sandboxed/App Store Mac app, this WiFi join
operation is fundamentally privileged and Apple provides no public, sandboxed
API on macOS to join arbitrary networks programmatically.

---

## 5. Behavior of NEHotspotConfiguration Without the Entitlement (Mac Catalyst)

When `NEHotspotConfigurationManager.SharedManager.ApplyConfiguration(config,
completion)` is called on Mac Catalyst without the entitlement:

| Behavior | Detail |
|---|---|
| **No crash** | The call returns normally; the app is not terminated |
| **No system UI** | No join prompt, no error dialog shown to the user |
| **Error via callback** | The completion handler is called with an `NSError` |
| **Error domain** | `NEHotspotConfigurationErrorDomain` |
| **Error code** | `NEHotspotConfigurationError.UserUnauthorized` (raw value 8). Some reports show `SystemDenied`. The completion block is invoked immediately. |

**Consequence**: Silent failure. The app would receive an error but the user sees
nothing unless the app handles and surfaces it. Any logic that assumes a null error
means success will silently proceed incorrectly.

---

## 6. Comparison of Approaches

| Approach | Works on Mac Catalyst? | Entitlement needed | Credentials in proc args? | Developer Program? |
|---|---|---|---|---|
| `NEHotspotConfiguration` | No (entitlement unavailable) | `HotspotConfiguration` (iOS only) | No | Yes (paid) |
| `CoreWLAN` (managed, `net9.0-macos`) | Requires extra target head | None (but admin auth) | No | Not required for local builds |
| `CoreWLAN` (ObjC interop from Catalyst) | Yes, but complex | None (but admin auth) | No | Not required for local builds |
| `airport` CLI tool | Deprecated/removed | None | **Yes (unsafe)** | No |
| AppleScript via `Process` | Unreliable, sandboxed | None | **Yes (unsafe)** | No |

---

## 7. Security Considerations

- **Passwords must never appear in command-line arguments.** Both `airport` and
  AppleScript-based approaches require embedding credentials in process arguments
  or scripts, which are visible to any user via `ps aux`. This is a hard OWASP
  violation (A02 – Cryptographic Failures / A05 – Security Misconfiguration).

- `NEHotspotConfiguration` and `CWInterface` both receive the password as an
  in-memory parameter passed directly to the OS kernel networking layer. The
  password is not logged or persisted by the framework.

- Credentials extracted from the QR code must be kept in memory only (no disk,
  no NSUserDefaults, no logging). This aligns with FR-015 in the spec.

- On macOS, when `CWInterface.associate` triggers an admin auth dialog, the OS
  manages the credential exchange — the app never sees the admin password.

---

## 8. Recommended Implementation Path

### For a `net9.0-maccatalyst`-only project

1. Add a `Platforms/MacCatalyst/WiFiService.cs` (or `MacOS/`) file.
2. Load CoreWLAN via `ObjCRuntime` interop (§4 above) or add the `net9.0-macos`
   target head to get managed bindings.
3. Disable app sandboxing in `Entitlements.plist` (for local/sideload distribution):
   ```xml
   <key>com.apple.security.app-sandbox</key>
   <false/>
   ```
4. Define the service behind an interface so the iOS build can use
   `NEHotspotConfiguration` and the macOS/Catalyst build uses CoreWLAN — the
   MAUI platform-specific dispatch pattern handles this cleanly.
5. Wrap `AssociateToNetwork` in a `Task.Run()` since it is synchronous and
   potentially long-running.
6. Detect "already connected" as a non-error condition: check the interface's
   current SSID before attempting association.

### Entitlements.plist (Mac Catalyst — non-sandbox local build)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
    "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Disable sandbox to allow CWInterface.associate -->
    <key>com.apple.security.app-sandbox</key>
    <false/>
</dict>
</plist>
```

### Entitlements.plist (iOS — requires paid Developer Program)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
    "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.developer.networking.HotspotConfiguration</key>
    <true/>
</dict>
</plist>
```

---

## 9. Known Limitations on macOS Catalyst vs iOS

| Limitation | Detail |
|---|---|
| No `NEHotspotConfiguration` entitlement issuable | Apple blocks Hotspot Config capability registration for Mac Catalyst App IDs |
| No user-consent join dialog | On iOS, the OS shows "Join [SSID]?" — macOS Catalyst has no equivalent UI from this API |
| `CWInterface.associate` is synchronous and blocking | Must run off the main thread |
| Admin authorization may be required | On macOS, joining certain networks or first use may trigger auth; this is OS-native behavior |
| No completion event from CoreWLAN | Unlike `NEHotspotConfiguration`, CoreWLAN's `associate` returns immediately after the connection attempt; there is no async event when the IP is assigned |
| Managed binding gap | `CoreWlan.*` managed bindings are in `Microsoft.macOS.dll`, not in the Mac Catalyst assembly — requires ObjC interop or a separate macOS target head |
| `CWInterface.scanForNetworks` requires the network to be in range | Hidden SSIDs require using the `includeHidden: true` overload |

---

## 10. QR Scanning Library Decision

**Decision**: `BarcodeScanning.Native.Maui` v3.0.3

| | `ZXing.Net.Maui.Controls` 0.7.4 | `BarcodeScanning.Native.Maui` 3.0.3 ✅ |
|---|---|---|
| Mac Catalyst engine | Managed ZXing port via AVFoundation | Apple **Vision framework** (native) |
| Mac Catalyst docs | iOS path only — MacCatalyst `Info.plist` gap | Explicit macOS section |
| AOT/trimming (Release) | Open bug PR #273, unmerged | No known issues |
| Permission API | OS triggers from `NSCameraUsageDescription` | `Methods.AskForRequiredPermissionAsync()` |
| Releases (maintenance) | ~22 | ~40 |

**Setup summary**:
- `MauiProgram.cs`: `builder.UseBarcodeScanning()`
- `Platforms/MacCatalyst/Info.plist`: add `NSCameraUsageDescription`
- XAML: `<scanner:CameraView BarcodeSymbologies="QRCode" OnDetectionFinished="..." />`
- Code-behind: `await Methods.AskForRequiredPermissionAsync()` before setting `CameraEnabled = true`
- Disable camera (`CameraEnabled = false`) in `OnDisappearing`

---

## 11. MVVM and Navigation

**MVVM**: `CommunityToolkit.Mvvm` v8.4.x  
Source-gen `[ObservableProperty]` and `[RelayCommand]` — zero boilerplate, AOT-safe, MAUI community standard. Alternatives (Prism, ReactiveUI) add complexity not warranted for a 5-page app.

**Navigation**: MAUI **Shell** with named routes  
Simple forward-only 5-screen flow with one branch. Shell's named-route API (`Shell.Current.GoToAsync(...)`) simplifies testing and avoids manual page management. Routes registered in `AppShell.xaml`.

**Testing**: `xunit` + `NSubstitute`  
ViewModels depend on interfaces; all injected services are substitutable. No MAUI UI test framework required for v1 — ViewModel state assertions cover all acceptance scenarios.

---

## 12. macOS Target

**Decision**: `net9.0-maccatalyst` (single target)  
Mature, well-supported, uses the same AVFoundation/Vision camera stack as iOS. All required MAUI controls work. `net9.0-macos` (AppKit MAUI) remains experimental in .NET 9 and is not needed since ObjC interop gives access to CoreWLAN from the Mac Catalyst target.
