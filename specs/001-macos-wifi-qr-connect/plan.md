# Implementation Plan: macOS WiFi QR Connect

**Branch**: `001-macos-wifi-qr-connect` | **Date**: 2026-03-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-macos-wifi-qr-connect/spec.md`

## Summary

macOS desktop application (C# / .NET MAUI, `net9.0-maccatalyst`) that activates the built-in camera via `BarcodeScanning.Native.Maui` (Apple Vision), continuously scans for QR codes, parses any detected `WIFI:` URI payload in-process, presents a confirmation dialog, and joins the WiFi network via CoreWLAN ObjC runtime interop — entirely offline, with zero credential persistence.

## Technical Context

**Language/Version**: C# 13 / .NET 9  
**Framework**: .NET MAUI `net9.0-maccatalyst`  
**Primary Dependencies**: `BarcodeScanning.Native.Maui` 3.0.3 (QR scanning), `CommunityToolkit.Mvvm` 8.4.x (MVVM), `CommunityToolkit.Maui` (latest, helpers)  
**Storage**: N/A — no persistence; all data is transient in-memory only  
**Testing**: `xunit` + `NSubstitute` (unit + ViewModel tests; `net9.0` test project)  
**Target Platform**: macOS 13 Ventura+ (Mac Catalyst)  
**Project Type**: Desktop GUI application (single-window, 5 pages)  
**Performance Goals**: QR code detected within 3 seconds under normal indoor lighting (SC-002); full scan-to-connect under 30 seconds (SC-001)  
**Constraints**: Offline-only; non-sandboxed (CoreWLAN requirement); no outbound network from the app itself; English only  
**Scale/Scope**: Single-user local utility; 5 pages; 3 service interfaces; 1 platform-specific implementation

## Constitution Check

*GATE: Pre-design evaluation (post-research re-check below)*

- [x] **I. Security First** — QR payload is untrusted external input. `QrParserService` validates with a strict regex against the `WIFI:` schema; only known fields (SSID, SecurityType, Password, IsHidden) are extracted. Password is passed in-process to CoreWLAN OS API — never placed in CLI args, in a log, or in a `ToString()`. Threat model documented in [research.md §Threat Model](research.md).
- [x] **II. UX Consistency** — Loading state: `ConnectingPage` with `ActivityIndicator`; Success state: `ResultPage` (success variant, SSID + checkmark); Error state: `ResultPage` (failure variant, plain-language Reason + Retry). All interactive elements will carry `AutomationId` and `SemanticProperties.Description` for accessibility. `PermissionDeniedPage` handles the camera-denied branch.
- [x] **III. Code Quality** — Each class has one responsibility: `QrParserService` (parsing only), `CoreWlanWifiConnector` (OS WiFi join only), ViewModels (per-page state only). `IWifiConnector` / `IQrParserService` / `ICameraPermissionService` keep platform code behind interfaces. YAGNI: no speculative features beyond the 5 defined user flows.
- [x] **IV. Privacy by Design** — No disk writes at any point. No analytics/crash-reporting SDK. `WifiCredential` never serialised; overrides `ToString()` to omit password. Memory lifetime documented in [data-model.md](data-model.md).
- [x] **V. Testability** — `QrParserService` is pure logic and covered by unit tests; ViewModels are tested with substituted services via `NSubstitute`; user-facing flows (scanner, result, and permission-denied paths) are additionally covered by integration tests that use fake scanner input and fake services instead of real camera hardware or live network access.

*Post-design re-check*: No new violations introduced by the data model or service design.

## Project Structure

### Documentation (this feature)

```text
specs/001-macos-wifi-qr-connect/
├── plan.md          ← this file
├── research.md      ← Phase 0: all technical decisions + threat model
├── data-model.md    ← Phase 1: entities, state transitions, lifetime
├── quickstart.md    ← Phase 1: build/run/test instructions
└── tasks.md         ← Phase 2 output (/speckit.tasks command)
```

*Contracts directory skipped: this is a closed-loop desktop app with no external-facing API, CLI schema, or inter-service interface exposed to other systems.*

### Source Code (repository root)

```text
QrWifiConnect/
├── QrWifiConnect.sln
├── src/
│   └── QrWifiConnect/
│       ├── QrWifiConnect.csproj          (net9.0-maccatalyst)
│       ├── MauiProgram.cs
│       ├── App.xaml / App.xaml.cs
│       ├── AppShell.xaml / AppShell.xaml.cs
│       ├── Models/
│       │   ├── WifiCredential.cs         record; password omitted from ToString()
│       │   ├── WifiSecurityType.cs       enum: Wpa | Wpa3 | Wep | Open
│       │   └── ConnectionResult.cs       sealed: Success | Failure
│       ├── Services/
│       │   ├── IQrParserService.cs
│       │   ├── QrParserService.cs        parses WIFI: URI; regex-validated
│       │   ├── IWifiConnector.cs
│       │   └── ICameraPermissionService.cs
│       ├── ViewModels/
│       │   ├── ScannerViewModel.cs
│       │   ├── ConfirmationViewModel.cs
│       │   ├── ConnectingViewModel.cs
│       │   ├── ResultViewModel.cs
│       │   └── PermissionDeniedViewModel.cs
│       ├── Views/
│       │   ├── ScannerPage.xaml / .cs
│       │   ├── ConfirmationPage.xaml / .cs
│       │   ├── ConnectingPage.xaml / .cs
│       │   ├── ResultPage.xaml / .cs
│       │   └── PermissionDeniedPage.xaml / .cs
│       └── Platforms/
│           └── MacCatalyst/
│               ├── AppDelegate.cs
│               ├── Info.plist            NSCameraUsageDescription
│               ├── Entitlements.plist    app-sandbox = false
│               └── CoreWlanWifiConnector.cs   IWifiConnector via ObjCRuntime
└── tests/
    └── QrWifiConnect.Tests/
        ├── QrWifiConnect.Tests.csproj    (net9.0)
        ├── QrParserServiceTests.cs
        ├── ViewModels/
        │   ├── ScannerViewModelTests.cs
        │   ├── ConfirmationViewModelTests.cs
        │   └── ResultViewModelTests.cs
        └── Fakes/
            ├── FakeWifiConnector.cs
            └── FakeCameraPermissionService.cs
```

**Structure Decision**: Single-project MAUI app (`src/QrWifiConnect`) with a co-located test project (`tests/QrWifiConnect.Tests`). The platform-specific WiFi connector lives exclusively in `Platforms/MacCatalyst/` using `#if MACCATALYST` guards. No multi-targeting or extra project heads required.

## Complexity Tracking

> No constitution violations. No complexity justification required.

