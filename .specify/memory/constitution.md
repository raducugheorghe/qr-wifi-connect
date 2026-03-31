<!--
SYNC IMPACT REPORT
==================
Version change: (unversioned template) → 1.0.0
Bump type: MINOR — first population of all principles and governance sections from template.

Principles added (new):
  - I. Security First (NON-NEGOTIABLE)
  - II. UX Consistency
  - III. Code Quality
  - IV. Privacy by Design
  - V. Testability (NON-NEGOTIABLE)

Sections added:
  - Security & Privacy Standards
  - Development Workflow & Quality Gates
  - Governance

Templates status:
  ✅ .specify/templates/plan-template.md — Constitution Check gates updated to reflect principles
  ✅ .specify/templates/spec-template.md — Existing structure aligns with principles; no changes required
  ✅ .specify/templates/tasks-template.md — Phase 2 foundational tasks cover auth, error handling, logging; aligns with principles

Deferred TODOs: None
-->

# QrScanner Constitution

## Core Principles

### I. Security First (NON-NEGOTIABLE)

Security is a non-negotiable gate, not an afterthought. Every feature MUST be reviewed for attack surface before implementation begins.

- All data received from QR code payloads MUST be treated as untrusted input; URLs and deep-links MUST be validated and sanitised before any action is taken.
- Camera and device permissions MUST be requested with the minimum scope required and MUST present clear rationale to the user at the point of request.
- No credential, token, or sensitive scan payload may be logged, written to disk, or transmitted without explicit user consent.
- Third-party dependencies MUST be audited for known CVEs before adoption and pinned to a verified version.
- Security defects are P0 and MUST block release regardless of other pending work.

### II. UX Consistency

The user experience MUST be predictable, coherent, and accessible across all screens and states.

- All interactive controls MUST follow the platform's Human Interface Guidelines (iOS/Android) or the project's established design system — whichever is more specific.
- Every asynchronous operation MUST surface a visible loading state, a success state, and a distinct error state with a human-readable message and a recovery action where possible.
- Error messages MUST describe what went wrong in plain language and MUST NOT expose internal stack traces or system identifiers to the user.
- Navigation patterns (back behaviour, modal dismissal, deep-link routing) MUST be consistent throughout the application.
- Accessibility: all interactive elements MUST have descriptive labels; minimum contrast ratio of 4.5:1 MUST be maintained; dynamic type MUST be supported.

### III. Code Quality

Code is a long-term asset; clarity and maintainability MUST be valued over cleverness.

- Each module or class MUST have a single, clearly documented responsibility (Single Responsibility Principle).
- Public interfaces MUST be small and stable; internal implementation details MUST NOT leak across module boundaries.
- Duplication MUST be eliminated — extract shared logic into a reusable unit before the third copy would be created (DRY).
- New functionality MUST NOT be added speculatively; implement what is required now (YAGNI).
- All code changes MUST pass static analysis and linting at the configured strictness level before merge; warnings are treated as errors.

### IV. Privacy by Design

User privacy is a first-class product requirement, not a compliance checkbox.

- Scan history and QR content MUST be stored on-device only, unless the user explicitly opts in to cloud sync.
- The application MUST NOT transmit scan payloads, device identifiers, or usage telemetry to any external service without prior informed consent presented in plain language.
- Data retention: any locally stored scan data MUST be deletable by the user at any time from within the application.
- Features that require personalisation or analytics MUST default to off; opt-in MUST be granular and revocable.

### V. Testability (NON-NEGOTIABLE)

Every feature MUST be demonstrably correct through automated tests before it ships.

- New business logic MUST be covered by unit tests before the implementing pull request is merged.
- User-facing flows (scan, result display, permission prompts, error states) MUST have UI or integration tests.
- Tests MUST be deterministic and MUST NOT depend on network access or real camera hardware in CI; use dependency injection and fakes/mocks at system boundaries.
- A green test suite is a required merge gate; flaky tests MUST be fixed or quarantined immediately — they MUST NOT be ignored.

## Security & Privacy Standards

The following standards apply to all features and MUST be verified during code review:

- **Input validation**: Every external input (QR payload, URL scheme, deep-link parameter) MUST be validated against an allowlist or strict schema before processing.
- **Dependency management**: The dependency lock file MUST be committed and regenerated only through a reviewed PR; automated vulnerability scanning MUST run on every CI build.
- **Permission hygiene**: The application MUST declare only the permissions actually used; unused permission declarations MUST be removed.
- **Secure storage**: Any sensitive data stored on-device (e.g., auth tokens) MUST use the platform's secure keychain/keystore API — plain files or shared preferences are prohibited for sensitive values.
- **Network**: All outbound network requests MUST use TLS 1.2 or higher; certificate pinning SHOULD be applied for first-party API endpoints.

## Development Workflow & Quality Gates

All pull requests MUST satisfy the following gates before merging:

1. **Constitution Check** — reviewer confirms the PR does not violate any principle in this document.
2. **Tests pass** — the full automated test suite is green with no skipped tests related to the changed code.
3. **Static analysis** — linter/static analyser reports zero errors at the project's configured strictness.
4. **Security review** — any feature touching permissions, external input, storage, or networking MUST include a note confirming Security First and Privacy by Design compliance.
5. **UX review** — any feature touching UI MUST confirm loading/success/error states are implemented and accessible labels are present.

Branch strategy: all work MUST be done on feature branches; direct commits to the main branch are prohibited.

## Governance

This constitution supersedes all other development practices documented elsewhere. Conflicts MUST be resolved in favour of this document until an amendment is ratified.

**Amendment procedure**:

1. Propose the change as a pull request modifying `.specify/memory/constitution.md`.
2. State the version bump type (MAJOR / MINOR / PATCH) and rationale in the PR description.
3. At least one reviewer must explicitly approve the constitutional change.
4. Update `LAST_AMENDED_DATE` and `CONSTITUTION_VERSION` in the same commit.
5. Propagate changes to dependent templates (plan, spec, tasks) in the same PR.

**Versioning policy**: MAJOR for principle removal or backward-incompatible redefinition; MINOR for new principle or section; PATCH for clarifications and wording fixes.

**Compliance review**: Constitution Check is a required step in every PR review. Quarterly, the team MUST review this document and confirm it still reflects current project realities.

**Version**: 1.0.0 | **Ratified**: 2026-03-31 | **Last Amended**: 2026-03-31
