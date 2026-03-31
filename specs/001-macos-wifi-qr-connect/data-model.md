# Data Model: macOS WiFi QR Connect

**Branch**: `001-macos-wifi-qr-connect` | **Phase**: 1 | **Date**: 2026-03-31

All entities are **transient / in-memory only**. Nothing is written to disk, a database, or any external service (FR-015, FR-016). The data model describes the shape of objects that live in memory during a single scan-to-connect flow and are discarded after the flow ends.

---

## Entities

### WifiCredential

Represents the decoded, validated contents of a `WIFI:` QR payload. Created by `QrParserService`, consumed by `ConfirmationViewModel` and `WifiConnector`. Discarded immediately after the connection attempt completes.

| Field | Type | Required | Validation rule |
|-------|------|----------|-----------------|
| `Ssid` | `string` | Yes | Non-empty, max 32 UTF-8 characters (IEEE 802.11 limit) |
| `SecurityType` | `WifiSecurityType` | Yes | One of the defined enum values |
| `Password` | `string?` | Only when `SecurityType != Open` | Null for open networks; non-empty when required |
| `IsHidden` | `bool` | No | Default `false`; `true` when QR encodes `H:true` |

**Invariant**: `Password` must be non-null and non-empty when `SecurityType` is `Wpa`, `Wpa3`, or `Wep`; must be null for `Open`.

**Privacy convention**: `ToString()` returns only the SSID (e.g. `WifiCredential(SSID=MyNetwork, SecurityType=Wpa)`) — the password is never serialised to a string, logged, or displayed anywhere.

---

### WifiSecurityType (Enum)

Security types supported by the `WIFI:` QR code standard.

| Value | `WIFI:T:` token | Notes |
|-------|-----------------|-------|
| `Wpa` | `WPA` | WPA Personal / WPA2 Personal — covers most home/office networks |
| `Wpa3` | `WPA3` | Some routers emit `WPA3` explicitly |
| `Wep` | `WEP` | Legacy; included for completeness |
| `Open` | `nopass` or empty | No password required |

---

### ConnectionResult (Sealed hierarchy)

Represents the outcome of a single WiFi join attempt. Created by `WifiConnector`, passed to `ResultViewModel` to drive the result screen. Discarded when the user navigates away from the result screen.

#### ConnectionResult.Success

| Field | Type | Notes |
|-------|------|-------|
| `Ssid` | `string` | The SSID the device connected to |

#### ConnectionResult.Failure

| Field | Type | Notes |
|-------|------|-------|
| `Ssid` | `string` | The SSID that was attempted |
| `Reason` | `string` | Plain-language message shown to user |
| `IsTimeout` | `bool` | `true` when the OS timed out; drives the timeout-specific message |

---

### ScanState (Enum)

Represents the camera state, used within `ScannerViewModel` to drive page rendering.

| Value | Meaning |
|-------|---------|
| `Scanning` | Camera active, scanning for QR codes |
| `Paused` | Camera active but scanning paused (QR detected, awaiting navigation) |
| `PermissionDenied` | Camera permission denied; show guidance screen |
| `CameraUnavailable` | Hardware not accessible (taken by another app, error) |

---

## Entity Relationships

```
ScannerViewModel
  └─── detects QR ──► QrParserService
                           └─── parses ──► WifiCredential
                                               │
                           passed via Shell route query params
                                               ↓
                        ConfirmationViewModel (shows SSID, security type)
                           └─── on Connect ──► WifiConnector
                                                    └─── produces ──► ConnectionResult
                                                                            │
                                              passed via Shell route query params
                                                                            ↓
                                                                 ResultViewModel
```

---

## State Transitions

### Scanner Flow

```
App Launch
    │
    ▼
[Check camera permission]
    │
    ├─ Denied ──► ScannerPage (PermissionDenied UI) ──► Open System Settings
    │
    └─ Granted ──► ScannerPage (Scanning)
                       │
                       ├─ Non-WiFi QR detected ──► [ignore, continue scanning]
                       │
                       ├─ Invalid WIFI: payload ──► [ignore, continue scanning]
                       │
                       └─ Valid WiFi QR ──► navigate to ConfirmationPage
```

### Confirmation and Result Flow

```
ConfirmationPage
    │
    ├─ Cancel ──► back to ScannerPage (Scanning)
    │
    └─ Connect ──► ConnectingPage (spinner)
                       │
                       ├─ Success ──► ResultPage (success) ──► Exit
                       │
                       └─ Failure ──► ResultPage (failure, reason)
                                           │
                                           ├─ Retry ──► ScannerPage (Scanning)
                                           └─ Exit ──► App quits
```

---

## WifiCredential Lifetime

```
QR Detected
    │
    ▼
QrParserService.TryParse() ──► WifiCredential created (heap)
    │
    ├─ ConfirmationViewModel holds reference
    │
    │  [user taps Connect]
    │
    ├─ WifiConnector.ConnectAsync(credential) called
    │
    │  [connection attempt completes]
    │
    ├─ ConnectionResult created
    │
    ├─ WifiCredential out of scope ◄── GC eligible; password field nulled first
    │
    ├─ ResultViewModel holds ConnectionResult (no credential fields)
    │
    └─ [user taps Retry or Exit]
           ConnectionResult out of scope ◄── GC eligible
```

No credential field survives beyond the `ConnectingViewModel` completing.
