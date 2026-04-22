# QR WiFi Connect

> 🤖 This app was vibecoded using [spec-kit](https://github.com/github/spec-kit) and [GitHub Copilot](https://github.com/features/copilot).

A **.NET MAUI Mac Catalyst** app that scans a WiFi QR code with the device camera and joins the encoded network automatically — no typing required.

## Features

- Live camera viewfinder with real-time QR detection
- Parses the standard `WIFI:` QR payload (WPA/WPA2, WPA3, WEP, and open networks; hidden SSIDs)
- Confirmation screen before connecting — shows SSID, security type, and hidden-network status
- Joins the network via `networksetup -setairportnetwork` (the only reliable non-root path on Mac Catalyst)
- Graceful error messages with full diagnostic detail if the join fails
- Camera permission flow with re-check on app resume

## Architecture

```
src/QrWifiConnect/
├── Models/          WifiCredential, ConnectionResult, WifiSecurityType, ScanState
├── Services/        IQrParserService, IWifiConnector, ICameraPermissionService, INavigationService
├── ViewModels/      MVVM (CommunityToolkit.Mvvm): Scanner, Confirmation, Connecting, Result, PermissionDenied
├── Views/           XAML pages
└── Platforms/
    └── MacCatalyst/ CoreWlanWifiConnector (scan via CoreWLAN, join via networksetup)

tests/QrWifiConnect.Tests/
    xUnit + NSubstitute — compiles against shared source without the MAUI workload
```

## Dependencies

### Runtime

| Package | Version | Purpose |
|---------|---------|---------|
| [Microsoft.Maui.Controls](https://dot.net/maui) | 10.0.20 | UI framework |
| [BarcodeScanning.Native.Maui](https://github.com/afriscic/BarcodeScanning.Native.Maui) | 3.0.3 | Native camera + QR decoding |
| [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui) | 9.x | MAUI helpers and converters |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.x | Source-generated MVVM (ObservableObject, RelayCommand) |

### System (macOS — no installation required)

| Tool / Framework | Used for |
|------------------|---------|
| `CoreWLAN.framework` | Scanning for the target SSID before joining |
| `/usr/sbin/networksetup` | Joining the WiFi network (`-setairportnetwork <device> <ssid> [password]`) |

### Test

| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.x | Test framework |
| NSubstitute | 5.x | Mocking |
| Microsoft.NET.Test.Sdk | 17.x | Test runner integration |

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| macOS 13 (Ventura) or later | Required by Mac Catalyst 16+ |
| [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) | `global.json` pins to `10.0.101`, rolls forward to latest minor |
| Xcode + Command Line Tools | `xcode-select --install` |
| MAUI workload | `dotnet workload install maui` |
| Built-in or USB Wi-Fi adapter | Required to scan and join networks |

## Running the App

A helper script handles dependency checks, restore, build, and launch in one step:

```zsh
./run-app.zsh          # Debug (default)
./run-app.zsh Release  # Release
```

Or run the steps manually:

```zsh
# 1. Restore packages
dotnet restore src/QrWifiConnect/QrWifiConnect.csproj

# 2. Build
dotnet build src/QrWifiConnect/QrWifiConnect.csproj \
  -f net10.0-maccatalyst -c Debug

# 3. Run
dotnet run --project src/QrWifiConnect/QrWifiConnect.csproj \
  -f net10.0-maccatalyst -c Debug
```

## Running the Tests

The test project targets `net9.0` and compiles directly against the shared source files — **the MAUI workload is not required to run tests**.

```zsh
dotnet test tests/QrWifiConnect.Tests/QrWifiConnect.Tests.csproj
```

## WiFi QR Code Format

The app parses the standard `WIFI:` QR payload:

```
WIFI:T:<security>;S:<ssid>;P:<password>;H:<hidden>;;
```

| Field | Values | Description |
|-------|--------|-------------|
| `T` | `WPA`, `WPA3`, `WEP`, `nopass` | Security type |
| `S` | any string | Network SSID |
| `P` | any string | Password (omitted for open networks) |
| `H` | `true` / `false` | Hidden SSID (optional) |

## Security Notes

- **Password in process args**: `networksetup` receives the password as a command-line argument. It is momentarily visible to same-user processes via `ps`. This is an inherent limitation — `networksetup` has no stdin interface. The argument is cleared as soon as `networksetup` exits (typically under 2 seconds). The password never appears in logs or stdout.
- **App sandbox disabled**: `Entitlements.plist` sets `com.apple.security.app-sandbox = false`. This is required for CoreWLAN and `networksetup` to function.
