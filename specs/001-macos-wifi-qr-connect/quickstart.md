# Quickstart: macOS WiFi QR Connect

**Branch**: `001-macos-wifi-qr-connect` | **Date**: 2026-03-31
**Target**: macOS 13 Ventura+ (Mac Catalyst, `net9.0-maccatalyst`)
**Tools**: .NET 9 SDK, Xcode 16+, Visual Studio 2022 for Mac or `dotnet` CLI

---

## Prerequisites

| Requirement | Version | Check |
|-------------|---------|-------|
| .NET SDK | 9.0+ | `dotnet --version` |
| Xcode | 16.0+ | `xcode-select -p` |
| MAUI workload | installed | `dotnet workload list` |
| macOS | 13 Ventura+ | System Settings → About |

```bash
# Install MAUI workload if missing
dotnet workload install maui
```

---

## Project Structure

```
QrWifiConnect/
├── QrWifiConnect.sln
├── src/
│   └── QrWifiConnect/
│       ├── QrWifiConnect.csproj            (net9.0-maccatalyst)
│       ├── MauiProgram.cs
│       ├── App.xaml[.cs]
│       ├── AppShell.xaml[.cs]
│       ├── Models/
│       │   ├── WifiCredential.cs
│       │   ├── WifiSecurityType.cs
│       │   └── ConnectionResult.cs
│       ├── Services/
│       │   ├── IQrParserService.cs
│       │   ├── QrParserService.cs
│       │   ├── IWifiConnector.cs
│       │   └── ICameraPermissionService.cs
│       ├── ViewModels/
│       │   ├── ScannerViewModel.cs
│       │   ├── ConfirmationViewModel.cs
│       │   ├── ConnectingViewModel.cs
│       │   ├── ResultViewModel.cs
│       │   └── PermissionDeniedViewModel.cs
│       ├── Views/
│       │   ├── ScannerPage.xaml[.cs]
│       │   ├── ConfirmationPage.xaml[.cs]
│       │   ├── ConnectingPage.xaml[.cs]
│       │   ├── ResultPage.xaml[.cs]
│       │   └── PermissionDeniedPage.xaml[.cs]
│       └── Platforms/
│           └── MacCatalyst/
│               ├── AppDelegate.cs
│               ├── Info.plist
│               ├── Entitlements.plist
│               └── CoreWlanWifiConnector.cs
└── tests/
    └── QrWifiConnect.Tests/
        ├── QrWifiConnect.Tests.csproj      (net9.0)
        ├── QrParserServiceTests.cs
        ├── ViewModels/
        │   ├── ScannerViewModelTests.cs
        │   ├── ConfirmationViewModelTests.cs
        │   └── ResultViewModelTests.cs
        └── Fakes/
            ├── FakeWifiConnector.cs
            └── FakeCameraPermissionService.cs
```

---

## Build

```bash
# Restore
dotnet restore

# Build (Mac Catalyst only)
dotnet build src/QrWifiConnect -f net9.0-maccatalyst

# Run tests (no hardware needed)
dotnet test tests/QrWifiConnect.Tests/
```

## Run on macOS

```bash
dotnet build src/QrWifiConnect -f net9.0-maccatalyst -t:Run
```

Or open the `.sln` in Visual Studio 2022 for Mac / Rider, select the **Mac Catalyst** scheme, and press Run.

---

## Key NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `BarcodeScanning.Native.Maui` | 3.0.3 | QR scanning via Apple Vision (native, Mac Catalyst) |
| `CommunityToolkit.Mvvm` | 8.4.x | Source-gen ObservableProperty + RelayCommand |
| `CommunityToolkit.Maui` | latest stable | MAUI community controls (optional helpers) |

Add to `QrWifiConnect.csproj`:
```xml
<PackageReference Include="BarcodeScanning.Native.Maui" Version="3.0.3" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.*" />
```

---

## Required Configuration Files

### Platforms/MacCatalyst/Info.plist — camera permission

```xml
<key>NSCameraUsageDescription</key>
<string>Camera access is required to scan WiFi QR codes.</string>
```

Without this key, macOS silently blocks camera access — no error, no frames.

### Platforms/MacCatalyst/Entitlements.plist — sandbox off

```xml
<key>com.apple.security.app-sandbox</key>
<false/>
```

Required for `CoreWLAN` (`CWInterface.associateToNetwork`) to join WiFi networks. Sandboxed apps cannot join arbitrary networks programmatically on macOS.

---

## MauiProgram.cs Bootstrap

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseBarcodeScanning()              // BarcodeScanning.Native.Maui
        .UseMauiCommunityToolkit();        // optional

    // Services
    builder.Services.AddSingleton<IQrParserService, QrParserService>();
    builder.Services.AddSingleton<ICameraPermissionService, CameraPermissionService>();

#if MACCATALYST
    builder.Services.AddSingleton<IWifiConnector, CoreWlanWifiConnector>();
#endif

    // ViewModels
    builder.Services.AddTransient<ScannerViewModel>();
    builder.Services.AddTransient<ConfirmationViewModel>();
    builder.Services.AddTransient<ConnectingViewModel>();
    builder.Services.AddTransient<ResultViewModel>();
    builder.Services.AddTransient<PermissionDeniedViewModel>();

    return builder.Build();
}
```

---

## App Flow

```
Launch
  ├─ Camera denied? ──► PermissionDeniedPage → [Open System Settings button]
  └─ Camera granted? ──► ScannerPage (live viewfinder)
                              │ WiFi QR detected
                              ▼
                         ConfirmationPage ("Connect to [SSID]?")
                              ├─ Cancel ──► back to ScannerPage
                              └─ Connect ──► ConnectingPage (spinner)
                                                ├─ Success ──► ResultPage ──► Exit
                                                └─ Failure ──► ResultPage ──► Retry | Exit
```

---

## Service Interfaces

```csharp
public interface IQrParserService
{
    // Returns null for non-WIFI: codes or invalid payloads
    WifiCredential? TryParse(string rawQrValue);
}

public interface IWifiConnector
{
    // Platform implementation: Platforms/MacCatalyst/CoreWlanWifiConnector.cs
    Task<ConnectionResult> ConnectAsync(WifiCredential credential,
        CancellationToken ct = default);
}

public interface ICameraPermissionService
{
    Task<bool> IsCameraPermissionGrantedAsync();
    Task<bool> RequestCameraPermissionAsync();
    void OpenSystemSettings();          // opens Privacy & Security › Camera
}
```

All interfaces are registered in DI — substitute with fakes in unit tests and integration tests. `ScannerPage` does not request permission directly; permission flow is owned by `ICameraPermissionService` and orchestrated by `ScannerViewModel`.

---

## Running Tests

```bash
dotnet test tests/QrWifiConnect.Tests/ --logger "console;verbosity=normal"
```

Tests cover:
- `QrParserService`: valid/invalid/malformed `WIFI:` payloads, open/hidden networks
- `ScannerViewModel`: non-WiFi ignored, valid QR triggers navigation, permission states
- `ConfirmationViewModel`: Connect navigates forward, Cancel navigates back
- `ResultViewModel`: success state, failure with retry, exit action
- Integration flows: scanner detection, permission-denied path, and result-state rendering without real camera or live network hardware

No network access or camera hardware is required in tests. `FakeWifiConnector`, fake scanner input, and `FakeCameraPermissionService` provide deterministic coverage of user-facing flows.
