---
description: "Task list for macOS WiFi QR Connect — net9.0-maccatalyst"
---

# Tasks: macOS WiFi QR Connect

**Input**: Design documents from `/specs/001-macos-wifi-qr-connect/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | data-model.md ✅ | quickstart.md ✅

**Tech stack**: C# 13 / .NET 9 · .NET MAUI `net9.0-maccatalyst` · `BarcodeScanning.Native.Maui` 3.0.3 · `CommunityToolkit.Mvvm` 8.4.x · `xunit` + `NSubstitute`

**Organisation**: Tasks grouped by user story — each story is an independently deliverable increment.

## Format: `[ID] [P?] [Story?] Description — file path`

- **[P]**: Parallelisable (operates on a different file from other [P] tasks in the same phase)
- **[US1/US2/US3]**: User story label
- File paths relative to solution root

---

## Phase 1: Setup (Project Scaffolding)

**Purpose**: Create the solution, projects, and all configuration files so the app builds on Mac Catalyst.

- [X] T001 Create solution and project structure: `QrWifiConnect.sln`, `src/QrWifiConnect/QrWifiConnect.csproj` (net9.0-maccatalyst), `tests/QrWifiConnect.Tests/QrWifiConnect.Tests.csproj` (net9.0)
- [X] T002 [P] Configure `src/QrWifiConnect/QrWifiConnect.csproj` — add NuGet references: `BarcodeScanning.Native.Maui` 3.0.3, `CommunityToolkit.Mvvm` 8.4.x, `CommunityToolkit.Maui` latest
- [X] T003 [P] Configure `tests/QrWifiConnect.Tests/QrWifiConnect.Tests.csproj` — add NuGet references: `xunit` 2.x, `NSubstitute` 5.x, `Microsoft.NET.Test.Sdk`; add `<Compile Include>` items for shared non-MAUI source files (Models, service interfaces, `QrParserService`, all ViewModels) from `src/QrWifiConnect` — avoids requiring the `maui-maccatalyst` workload to run tests; `RootNamespace` set to `QrWifiConnect` so types resolve correctly
- [X] T004 [P] Set `NSCameraUsageDescription` key in `src/QrWifiConnect/Platforms/MacCatalyst/Info.plist`
- [X] T005 [P] Create `src/QrWifiConnect/Platforms/MacCatalyst/Entitlements.plist` with `com.apple.security.app-sandbox = false`
- [X] T006 [P] Register `BarcodeScanning.Native.Maui` in `src/QrWifiConnect/MauiProgram.cs` via `builder.UseBarcodeScanning()`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Models, service interfaces, Shell routing, and DI registration — everything all three user stories share. No user story work starts until this phase is complete.

**⚠️ CRITICAL**: User story phases depend on these foundations.

- [X] T007 [P] Create `src/QrWifiConnect/Models/WifiSecurityType.cs` — enum: `Wpa`, `Wpa3`, `Wep`, `Open`
- [X] T008 [P] Create `src/QrWifiConnect/Models/WifiCredential.cs` — record with `Ssid`, `SecurityType`, `Password?`, `IsHidden`; `ToString()` omits password (privacy invariant)
- [X] T009 Create `src/QrWifiConnect/Models/ConnectionResult.cs` — abstract sealed hierarchy: `ConnectionResult.Success(Ssid)` and `ConnectionResult.Failure(Ssid, Reason, IsTimeout)` (depends on T007)
- [X] T010 [P] Create `src/QrWifiConnect/Services/IQrParserService.cs` — `WifiCredential? TryParse(string rawQrValue)`
- [X] T011 [P] Create `src/QrWifiConnect/Services/IWifiConnector.cs` — `Task<ConnectionResult> ConnectAsync(WifiCredential credential, CancellationToken ct)`
- [X] T012 [P] Create `src/QrWifiConnect/Services/ICameraPermissionService.cs` — `Task<bool> IsCameraPermissionGrantedAsync()`, `Task<bool> RequestCameraPermissionAsync()`, `void OpenSystemSettings()`
- [X] T013 Create `src/QrWifiConnect/Services/INavigationService.cs` (`GoToAsync`, `GoBackAsync`, `QuitApplication`) and `ShellNavigationService.cs` (thin wrapper delegating to `Shell.Current` and `Application.Current`; enables ViewModel testability). Create `src/QrWifiConnect/AppShell.xaml` and `AppShell.xaml.cs` — define Shell routes: `//scanner`, `confirmation`, `connecting`, `result`, `permissiondenied`; register all five pages (depends on T007–T012 so ViewModels can be injected)
- [X] T014 Register all services and ViewModels in `src/QrWifiConnect/MauiProgram.cs`: `IQrParserService` → `QrParserService` (singleton), `ICameraPermissionService` → `CameraPermissionService` (singleton); register platform `IWifiConnector` behind `#if MACCATALYST`; register all five ViewModels as transient (depends on T010–T012)

**Checkpoint**: Solution builds. No user story code yet, but DI, routes, and models are in place.

---

## Phase 3: User Story 1 — Scan WiFi QR Code and Connect (Priority: P1) 🎯 MVP

**Goal**: Live camera viewfinder that detects a WiFi QR code, parses it, shows a confirmation dialog, and initiates a WiFi join — the core end-to-end journey.

**Independent Test**: Launch app with camera granted; present a `WIFI:T:WPA;S:TestNet;P:pass123;;` QR code; verify confirmation page appears with SSID "TestNet". ViewModel tests run without hardware.

**Acceptance Scenarios covered**: US1-1, US1-2, US1-3, US1-4

### Implementation — User Story 1

- [X] T015 [P] [US1] Implement `src/QrWifiConnect/Services/QrParserService.cs` — strict regex parser for `WIFI:T:<type>;S:<ssid>;P:<password>;H:<hidden>;;`; returns `null` for non-WIFI: codes and invalid payloads; supports all security types; handles open (no password) and hidden flag; never exposes password in exceptions or logs
- [X] T016 [P] [US1] Implement `src/QrWifiConnect/Platforms/MacCatalyst/CoreWlanWifiConnector.cs` — two-phase `IWifiConnector`: **Phase 1** uses `objc_msgSend` P/Invokes into CoreWLAN to call `scanForNetworksWithName:error:` on `CWInterface` (verifies SSID is in range; returns `ConnectionResult.Failure` with diagnostic if not found). **Phase 2** invokes `/usr/sbin/networksetup -setairportnetwork <device> <ssid> [password]` (the only non-root, non-sandboxed join path on Mac Catalyst); WiFi device name parsed from `networksetup -listallhardwareports` with `"en0"` fallback. 30-second timeout covers both phases. **Why not `associateToNetwork:password:error:`?** Returns `tmpErr (-32767)` on Mac Catalyst — airportd rejects XPC association requests from non-native processes on macOS 13+. **Why not `NEHotspotConfigurationManager`?** Always returns `NEHotspotConfigurationError.Internal (8)` on macOS — the iOS neagent daemon is absent. **Security trade-off**: password appears as a CLI arg visible to same-UID processes via `ps(1)` for ~2 s; no stdin/pipe interface to `networksetup` exists. Risk documented in class header comment. Credential reference cleared after use.
- [X] T017 [US1] Implement `src/QrWifiConnect/Services/CameraPermissionService.cs` — owns camera permission checks, permission request flow using `Methods.AskForRequiredPermissionAsync()`, and `OpenSystemSettings()` via `Launcher.OpenAsync("x-apple.systempreferences:com.apple.preference.security?Privacy_Camera")`; this is the single permission abstraction consumed by `ScannerViewModel` (depends on T012)
- [X] T018 [US1] Implement `src/QrWifiConnect/ViewModels/ScannerViewModel.cs` — `ObservableObject`; exposes `ScanState`, handles `OnQrDetected(string raw)` (calls `IQrParserService.TryParse`; ignores non-WiFi; navigates to `//confirmation` with `WifiCredential` on match); checks permission on appearing; handles `CameraUnavailable` (depends on T008, T010, T012, T015, T017)
- [X] T019 [US1] Create `src/QrWifiConnect/Views/ScannerPage.xaml` and `ScannerPage.xaml.cs` — `CameraView` from `BarcodeScanning.Native.Maui`, `BarcodeSymbologies="QRCode"`, `OnDetectionFinished` wired to ViewModel; `CameraEnabled` bound to ViewModel scanning state; forwards detection and lifecycle events only; does not call permission APIs directly; `AutomationId` and `SemanticProperties.Description` on all interactive elements (depends on T018)
- [X] T019a [US1] Update `src/QrWifiConnect/Views/ScannerPage.xaml` to render a visible `Camera unavailable` state with recovery guidance when `ScanState == CameraUnavailable`, and automatically restore the live viewfinder when the camera becomes available again (depends on T019)
- [X] T020 [US1] Implement `src/QrWifiConnect/ViewModels/ConfirmationViewModel.cs` — `ObservableObject`; receives `WifiCredential` via Shell `QueryProperty`; exposes `Ssid`, `SecurityType`, `IsHidden`; `ConnectCommand` (navigates to `//connecting` passing credential); `CancelCommand` (navigates back to `//scanner`); clears credential reference after navigate (depends on T008, T011)
- [X] T021 [US1] Create `src/QrWifiConnect/Views/ConfirmationPage.xaml` and `ConfirmationPage.xaml.cs` — shows SSID, security type, hidden-network badge; Connect and Cancel buttons with `AutomationId`; binds to `ConfirmationViewModel` (depends on T020)

**Checkpoint**: US1 complete. Camera scans → confirmation dialog shows SSID → Connect initiates join flow.

---

## Phase 4: User Story 2 — Connection Result Feedback with Retry or Exit (Priority: P2)

**Goal**: Full-screen success/failure result screens with Retry and Exit actions.

**Independent Test**: `ResultViewModel` tests with `FakeWifiConnector` returning success and failure outcomes independently verify both result states, retry navigation, and exit behaviour.

**Acceptance Scenarios covered**: US2-1, US2-2, US2-3, US2-4

### Implementation — User Story 2

- [X] T022 [US2] Implement `src/QrWifiConnect/ViewModels/ConnectingViewModel.cs` — `ObservableObject`; receives `WifiCredential` via Shell `QueryProperty`; on appearing, calls `IWifiConnector.ConnectAsync` with `CancellationToken` (30-second timeout) in background; on result navigates to `//result` passing `ConnectionResult`; clears credential reference immediately after `ConnectAsync` returns (depends on T009, T011, T016)
- [X] T023 [US2] Create `src/QrWifiConnect/Views/ConnectingPage.xaml` and `ConnectingPage.xaml.cs` — `ActivityIndicator` centred; inert (no user actions); binds to `ConnectingViewModel` (depends on T022)
- [X] T024 [US2] Implement `src/QrWifiConnect/ViewModels/ResultViewModel.cs` — `ObservableObject`; receives `ConnectionResult` via Shell `QueryProperty`; exposes `IsSuccess`, `Ssid`, `Reason`, `IsTimeout`; `RetryCommand` (navigates `//scanner`, clears result); `ExitCommand` (calls `Application.Current.Quit()`) (depends on T009)
- [X] T025 [US2] Create `src/QrWifiConnect/Views/ResultPage.xaml` and `ResultPage.xaml.cs` — two visual states via `VisualStateManager`: Success (checkmark icon, SSID, single Exit button) and Failure (error icon, SSID, plain-language Reason label, Retry + Exit buttons); all interactive elements accessible; binds to `ResultViewModel` (depends on T024)

**Checkpoint**: US2 complete. ConnectingPage spins → ResultPage shows correct outcome with Retry/Exit wired up.

---

## Phase 5: User Story 3 — Camera Permission Handling (Priority: P3)

**Goal**: On first launch, system permission prompt appears. If denied, `PermissionDeniedPage` shows with a direct link to System Settings; resumes automatically when permission is granted.

**Independent Test**: `ScannerViewModel` tests with `FakeCameraPermissionService` returning denied state verify `PermissionDenied` scan state and `OpenSystemSettings` command are triggered independently of hardware.

**Acceptance Scenarios covered**: US3-1, US3-2, US3-3, US3-4

### Implementation — User Story 3

- [X] T026 [US3] Implement `src/QrWifiConnect/ViewModels/PermissionDeniedViewModel.cs` — `ObservableObject`; `OpenSettingsCommand` calls `ICameraPermissionService.OpenSystemSettings()` (depends on T012, T017)
- [X] T027 [US3] Create `src/QrWifiConnect/Views/PermissionDeniedPage.xaml` and `PermissionDeniedPage.xaml.cs` — camera icon, plain-language explanation, "Open System Settings" button with `AutomationId`; binds to `PermissionDeniedViewModel` (depends on T026)
- [X] T028 [US3] Update `ScannerViewModel` to re-check permission on `OnAppearing` (app resume path) and automatically transition from `PermissionDenied` to `Scanning` when permission is now granted, without requiring restart (depends on T018, T017)
- [X] T029 [US3] Update `ScannerViewModel` and Shell navigation so denied camera permission routes to `//permissiondenied`; when permission is later granted and the app resumes, navigate back to `//scanner` and reactivate scanning (depends on T018, T027, T028)

**Checkpoint**: US3 complete. Denied permission → PermissionDeniedPage → Open System Settings working; granting permission resumes scanner without restart.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Unit tests, accessibility audit, security hardening, and final build validation.

- [X] T030 [P] Write `tests/QrWifiConnect.Tests/QrParserServiceTests.cs` — cover: valid WPA payload, valid WPA3 payload, open/no-password payload, hidden SSID payload, non-WIFI: QR code (returns null), malformed WIFI: missing SSID (returns null), payload injection attempt (semicolons/special chars in SSID/password extracted literally, not executed), SSID max length edge case
- [X] T031 [P] Create `tests/QrWifiConnect.Tests/Fakes/FakeWifiConnector.cs`, `FakeCameraPermissionService.cs`, and `FakeNavigationService.cs` — configurable return values; `FakeNavigationService` tracks navigation `History` (route + params tuples), `LastRoute`, `LastParams`, and `QuitCallCount`; no hardware or Shell dependency
- [X] T032 [P] Write `tests/QrWifiConnect.Tests/ViewModels/ScannerViewModelTests.cs` — cover: non-WiFi QR ignored (no navigation), valid WiFi QR triggers navigation to confirmation, PermissionDenied state when service returns denied, CameraUnavailable state (depends on T031)
- [X] T032a [P] Write `tests/QrWifiConnect.Tests/ViewModels/ScannerCameraUnavailableTests.cs` — cover transition into `CameraUnavailable` and recovery back to `Scanning` (depends on T031)
- [X] T033 [P] Write `tests/QrWifiConnect.Tests/ViewModels/ConfirmationViewModelTests.cs` — cover: ConnectCommand navigates to connecting with credential, CancelCommand navigates back to scanner, credential reference cleared after Connect (depends on T031)
- [X] T034 [P] Write `tests/QrWifiConnect.Tests/ViewModels/ResultViewModelTests.cs` — cover: Success state sets IsSuccess=true + Ssid, Failure state sets IsSuccess=false + Reason + IsTimeout, RetryCommand navigates to scanner + clears result, ExitCommand invokes Application.Quit (depends on T031)
- [X] T034a [P] Write `tests/QrWifiConnect.Tests/Integration/ScannerFlowIntegrationTests.cs` — cover valid WiFi QR detection, non-WiFi QR ignore behaviour, and navigation to confirmation using fake scanner input (depends on T031)
- [X] T034b [P] Write `tests/QrWifiConnect.Tests/Integration/ResultFlowIntegrationTests.cs` — cover success and failure result states, Retry navigation, and Exit action wiring with fake connector outcomes (depends on T031)
- [X] T034c [P] Write `tests/QrWifiConnect.Tests/Integration/PermissionFlowIntegrationTests.cs` — cover denied permission rendering, Open System Settings action, and resume-to-scanning after permission grant (depends on T031)
- [X] T035 Verify `WifiCredential.ToString()` never includes password field — assert in `QrParserServiceTests` (security hardening, Principle I)
- [X] T036 [P] Accessibility audit: confirm every interactive element on all five pages has `AutomationId` and `SemanticProperties.Description`; verify `VisualStateManager` states on `ResultPage` are keyboard-accessible
- [X] T037 [P] Run `dotnet build -f net9.0-maccatalyst -c Release` and confirm zero warnings (Principle III — warnings treated as errors); fix any static-analysis issues
- [X] T038 [P] Run `dotnet test tests/QrWifiConnect.Tests/` and confirm all tests green; confirm no test skips related to changed code (Principle V merge gate)
- [X] T039 [P] Validate SC-002 in `specs/001-macos-wifi-qr-connect/quickstart.md` — run a timed decode check with a sample WiFi QR under normal indoor lighting and confirm detection occurs within 3 seconds
- [X] T040 [P] Validate SC-001 in `specs/001-macos-wifi-qr-connect/quickstart.md` — run a timed end-to-end scan-to-connect manual acceptance pass and confirm completion in under 30 seconds with camera permission pre-granted

---

## Dependencies (Story Completion Order)

```
Phase 1 (Setup)
    └─► Phase 2 (Foundational — models + interfaces + Shell + DI)
             └─► Phase 3 (US1 — scanner + parser + WiFi connector + confirmation)
                      └─► Phase 4 (US2 — connecting + result screens)
                               └─► Phase 5 (US3 — permission denied page + resume)
                                        └─► Phase 6 (Polish — tests + audit + build validation)
```

US2 depends on US1 (ConnectingViewModel receives credential from ConfirmationViewModel).  
US3 depends on US1 (`ScannerViewModel` already exists; US3 extends it with permission resume logic).  
Phase 6 tests depend on all ViewModels and services being implemented.

## Parallel Execution (within each phase)

| Phase | Parallelisable tasks |
|-------|----------------------|
| Phase 1 | T002, T003, T004, T005, T006 can all run in parallel after T001 |
| Phase 2 | T007, T008, T010, T011, T012 in parallel; T009 after T007; T013+T014 last |
| Phase 3 | T015 + T016 + T017 in parallel; T018 depends on T015+T017; T019 after T018; T019a after T019; T020 after T018; T021 after T020 |
| Phase 4 | T022 → T023 → T024 → T025 (sequential; each drives the next) |
| Phase 5 | T026 → T027; T028 after T018+T017; T029 after T027+T028 |
| Phase 6 | T030, T031, T035, T036, T037, T039, T040 in parallel; T032, T032a, T033, T034, T034a, T034b, T034c after T031 |

## Implementation Strategy (MVP First)

| Milestone | Phases | What you get |
|-----------|--------|-------------|
| **MVP** | 1 + 2 + 3 | App builds, camera scans, confirmation dialog, WiFi join initiated |
| **Complete** | + 4 | Success/failure result screens with Retry and Exit |
| **Polished** | + 5 | Permission-denied guidance with System Settings shortcut |
| **Shippable** | + 6 | Unit + integration coverage, accessibility, performance validation, Release build clean |

**Suggested first PR**: Phases 1–3 (T001–T021) — demonstrates the full core scan-to-connect journey end-to-end.
