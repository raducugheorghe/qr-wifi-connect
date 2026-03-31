# Feature Specification: macOS WiFi QR Connect

**Feature Branch**: `001-macos-wifi-qr-connect`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "Build a macOS application that can scan a WiFi QR code using the built-in camera, ask for confirmation to connect, connect to WiFi network and display a confirmation or failure screen with retry or exit application options. No data is stored and all the processing needs to be done on the local machine."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scan WiFi QR Code and Connect (Priority: P1)

A user opens the application on their Mac. The app presents a live camera viewfinder. The user holds their Mac's camera up to a printed or screen-displayed WiFi QR code. The app recognises it as a valid WiFi credential QR code, decodes the network name and security type, and immediately shows a confirmation dialog: "Connect to [Network Name]?" with a Connect and a Cancel option. The user taps Connect; the Mac joins the network. A success screen confirms the connection with the network name visible and an Exit button.

**Why this priority**: This is the entire core user journey. Every other story depends on the camera successfully scanning a QR code, so it must work end-to-end first.

**Independent Test**: Can be fully tested by launching the app, presenting a valid WiFi QR test code (e.g., saved on a second screen), confirming the dialog, and verifying the Mac successfuly joins the referenced network.

**Acceptance Scenarios**:

1. **Given** the app is open and camera permission is granted, **When** a valid WiFi QR code (WPA/WPA2/WPA3 password-protected network) is held in front of the camera, **Then** the app decodes the QR code within 3 seconds and displays a confirmation dialog showing the network SSID.
2. **Given** the confirmation dialog is visible, **When** the user chooses Connect, **Then** the app attempts to join the network and shows a progress indicator while the connection is being established.
3. **Given** the confirmation dialog is visible, **When** the user chooses Cancel, **Then** the dialog is dismissed and the live camera viewfinder is restored so the user can scan again.
4. **Given** the app is open, **When** a QR code that does NOT contain WiFi credentials is detected, **Then** the QR code is silently ignored and the camera continues scanning — no error is shown.

---

### User Story 2 - Connection Result Feedback with Retry or Exit (Priority: P2)

After the connection attempt completes (whether or not it succeeds), the user sees a clear, full-screen result. On success: the connected network name is displayed with a visual confirmation and an Exit button. On failure: a descriptive human-readable reason (e.g., "Could not connect to [Network Name]. The network may be out of range or the credentials may have changed.") is shown with two options — Retry (returns to the live scanner) and Exit (quits the application).

**Why this priority**: Completing the user journey requires knowing whether the connection worked. Without result feedback the application delivers no value beyond an OS-level attempt.

**Independent Test**: Testable independently by mocking the network join outcome (both success and failure) and verifying the correct result screen appears with the expected controls.

**Acceptance Scenarios**:

1. **Given** the connection attempt succeeds, **When** the result screen appears, **Then** it displays the network name, a success indicator, and a single Exit button.
2. **Given** the connection attempt fails, **When** the result screen appears, **Then** it displays the network name, a plain-language error description, a Retry button, and an Exit button.
3. **Given** the failure result screen is shown, **When** the user taps Retry, **Then** the app returns to the live camera viewfinder, ready to scan again.
4. **Given** either result screen is shown, **When** the user taps Exit, **Then** the application quits cleanly.

---

### User Story 3 - Camera Permission Handling (Priority: P3)

A user launches the app for the first time. macOS prompts for camera access. If the user denies permission, or if permission was previously denied, the app cannot show a useful viewfinder. Instead it shows a clear message explaining that camera access is required and provides a button that opens System Settings directly to the Privacy & Security > Camera section so the user can grant permission without searching through settings manually.

**Why this priority**: Without camera access the app cannot function, but the permission flow is a one-time onboarding concern and does not affect users who have already granted access.

**Independent Test**: Testable by running the app in a state where camera permission is denied and verifying the guidance screen appears with a working "Open System Settings" button.

**Acceptance Scenarios**:

1. **Given** the app has never been granted camera permission, **When** the app is launched, **Then** the system permission prompt is shown to the user before the viewfinder is displayed.
2. **Given** the user denied camera permission (either now or previously), **When** the app is in the foreground, **Then** the viewfinder is NOT shown; instead a message is displayed explaining camera access is required.
3. **Given** the camera permission denied screen is shown, **When** the user taps "Open System Settings", **Then** System Settings opens to the Camera privacy page.
4. **Given** the user later grants camera permission in System Settings and returns to the app, **When** the app resumes, **Then** the live viewfinder is activated automatically without requiring a restart.

---

### Edge Cases

- **QR code is not a WiFi credential code**: The app ignores non-WiFi QR codes silently; the scanner continues without interruption.
- **Malformed WiFi QR payload**: If a QR code uses the WiFi URI scheme (`WIFI:`) but contains invalid or missing fields (e.g., no SSID), the payload is rejected and the scanner resumes without attempting a connection or displaying a credential.
- **Open (passwordless) network**: A WiFi QR code for an open network (no password) is supported; the confirmation dialog omits the password field and proceeds normally.
- **Hidden SSID network**: If the QR code specifies a hidden network (`H:true`), the confirmation dialog notes that the network is hidden; connection proceeds using the encoded SSID.
- **Already connected to the scanned network**: If the Mac is already joined to the network in the QR code, the confirmation dialog informs the user and the "Connect" action is still available (to reconnect/refresh).
- **Camera becomes unavailable mid-session** (e.g., another app takes exclusive access): The viewfinder shows a "Camera unavailable" message; scanning resumes automatically when the camera is free.
- **Connection attempt times out**: If the OS does not return a result within a reasonable wait window, the failure screen is shown with a timeout-specific message.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST activate the built-in Mac camera and display a live viewfinder as its primary screen.
- **FR-002**: The application MUST continuously scan the camera feed for QR codes and decode any detected code in real time.
- **FR-003**: The application MUST recognise and parse a WiFi credential QR code conforming to the standard `WIFI:T:<type>;S:<ssid>;P:<password>;H:<hidden>;;` format, supporting security types WPA, WPA2, WPA3, and open (nopass).
- **FR-004**: The application MUST validate the decoded WiFi payload before any connection is attempted; a payload missing a required field (SSID) MUST be silently rejected and scanning MUST resume.
- **FR-005**: The application MUST display a confirmation dialog showing at minimum the network name (SSID) before initiating any connection to the WiFi network.
- **FR-006**: The confirmation dialog MUST offer a Connect action and a Cancel action; selecting Cancel MUST return the user to the live scanner without attempting a connection.
- **FR-007**: The application MUST initiate the WiFi connection through the host macOS networking layer, using only the credentials extracted from the QR code.
- **FR-008**: The application MUST display a progress indicator while the connection attempt is in progress so the user knows an action is underway.
- **FR-009**: The application MUST display a success result screen identifying the connected network name when the connection succeeds.
- **FR-010**: The application MUST display a failure result screen with a plain-language description and both a Retry option and an Exit option when the connection fails.
- **FR-011**: The Retry action MUST return the user to the live camera viewfinder.
- **FR-012**: The Exit action MUST quit the application cleanly.
- **FR-013**: The application MUST request camera permission from the user on first launch and MUST provide a clear rationale for why camera access is required at the point of the system prompt.
- **FR-014**: If camera permission is denied, the application MUST display a guidance screen and MUST provide a direct shortcut to the relevant macOS System Settings page.
- **FR-015**: The application MUST NOT persist any decoded WiFi credential (SSID, password, security type) to disk, to a log, or to any external service, at any point.
- **FR-016**: All QR decoding and network join operations MUST be performed on the local machine with no network requests made by the application itself.
- **FR-017**: Non-WiFi QR codes detected by the camera MUST be ignored without any visible notification or interruption to scanning.

### Key Entities

- **WiFi Credential**: A transient, in-memory-only object representing the decoded contents of a WiFi QR code — network name (SSID), security type, password (optional), and hidden flag. It is never written to disk and is discarded immediately after the connection attempt completes (success or failure).
- **Camera Session**: Represents the active camera capture lifecycle — starting when the viewfinder is displayed, pausing when the app enters the background or loses camera permission, and stopping when the app quits.
- **Connection Result**: A transient value (success or failure with reason) produced by the network join attempt. Used solely to drive the result screen; not retained after the screen is dismissed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user with no prior knowledge of the app can go from launching the app to joining a new WiFi network in under 30 seconds, given camera permission is already granted.
- **SC-002**: The app detects and decodes a WiFi QR code within 3 seconds of the code being clearly visible in the viewfinder under normal indoor lighting.
- **SC-003**: 95% of users presented with both a success and a failure result screen correctly identify the next available action (exit or retry) without additional guidance.
- **SC-004**: Zero WiFi credentials (SSID or password) appear in any application log, crash report, or system log entry produced by the application.
- **SC-005**: All QR decoding and credential processing completes locally — no outbound network request is made by the application itself during any operation.
- **SC-006**: The application handles camera permission denial gracefully — 100% of users denied camera access see a recoverable guidance screen rather than a crash or blank screen.

## Assumptions

- The target macOS version is macOS 13 Ventura or later; all platform APIs required (camera access, QR decoding, WiFi joining) are available on this version and above.
- The application is a standalone, single-window macOS desktop app — not a menu bar item, browser extension, or CLI tool.
- The built-in camera (FaceTime HD or equivalent) is the only camera source; USB or external cameras are out of scope.
- WiFi QR codes follow the widely adopted `WIFI:` URI scheme; proprietary or non-standard encoding formats are out of scope.
- Enterprise/EAP WiFi networks (which require certificates or additional credentials beyond SSID/password) are out of scope for v1; only Personal (PSK) and open networks are supported.
- The app does not need to support connecting to 5 GHz vs 2.4 GHz band selection — that is handled by the operating system.
- The application is unsigned/sandboxed considerations are a technical concern for planning; the spec assumes the app will have the necessary entitlements to access the camera and join WiFi networks.
- No internationalisation (i18n) is required for v1; English is the only supported language.

