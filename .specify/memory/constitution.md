<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.1.0
- Modified principles: Operational Constraints, Development Governance
- Added sections: planned Document Upload and Management feature workflow requirements
- Removed sections: none
- Deferred items: application implementation remains deferred until the Spec Kit workflow produces an approved MVP plan and tasks.
-->
# ContosoDashboard Constitution

## Core Principles

### I. Quality Before Speed
All code MUST be readable, coherent, and easy to review. We prefer small, well-named abstractions over hidden coupling, and we keep business logic in service boundaries rather than mixing domain rules into Razor pages or data access code. Changes that add complexity without a clear purpose are not allowed. This project is explicitly a teaching application, so maintainability and traceability matter more than clever shortcuts.

### II. Security by Default
The application MUST enforce authorization at every protected boundary, including page access, service methods, and data retrieval. The training-only mock authentication system remains valid only for local, offline demonstration and MUST NOT be mistaken for a production identity implementation. Cookie-based auth, role checks, and defense-in-depth measures are required for the repo's learning goals, but any production replacement must use a real identity provider with password hashing, MFA, and secure session handling.

### III. Test and Validate Changes
Every non-trivial change MUST be supported by focused validation. Unit and integration tests are required for service logic, authorization rules, database behavior, and any security-sensitive path that affects user access or data integrity. The LocalDB and Blazor Server configuration MUST continue to work offline, and manual verification is acceptable only when it complements automated checks. Unverified security or data-access changes are not acceptable.

### IV. Maintainability Through Clear Boundaries
The repository MUST preserve separation of concerns across Models, Services, Data, and Pages. Infrastructure concerns such as database access, authentication, and future cloud integrations MUST be abstracted behind interfaces and dependency injection so the business layer remains portable. This keeps the training code understandable while preserving a clean migration path toward Azure SQL, Microsoft Entra ID, or storage services.

### V. Offline-First Training Constraints
This application MUST remain runnable without internet connectivity, cloud subscriptions, or external identity providers. The default runtime setup is ASP.NET Core 10 with Blazor Server, SQL Server LocalDB, and local file storage. Any infrastructure dependency introduced for learning purposes MUST be local-first and intentionally abstracted so production deployment can replace it with Azure-hosted services later without rewriting the domain logic.

### VI. Clean Abstractions for Future Production Integration
The codebase MUST favor abstraction layers over hard-coded infrastructure assumptions. Authentication, storage, and persistence integrations are expected to be swappable by configuration or implementation replacement. This includes the documented pattern of replacing local file storage with a cloud implementation via an interface such as `IFileStorageService`, while keeping business logic and UI contracts stable. Abstraction is a requirement for maintainability and migration readiness, not a temporary workaround.

## Operational Constraints

The following constraints are mandatory for this repository and supersede ad hoc shortcuts:

- .NET 10 with Blazor Server is the supported training stack and must remain compatible with the LocalDB-based development workflow.
- SQL Server LocalDB is the default development database and MUST remain usable without Azure or other external services.
- The cookie-based mock authentication model is approved only for training and demo use; it must remain isolated from production assumptions and clearly labeled as non-production.
- Document Upload and Management is the planned feature for this assignment. It MUST be specified, clarified, planned, decomposed into tasks, and implemented as an MVP through the repository's Spec Kit workflows: specification, clarification, plan, tasks, and MVP implementation. Application code MUST NOT be added before the relevant workflow artifacts establish the approved scope and acceptance criteria.
- Document Upload and Management MUST be implemented behind a storage abstraction, must validate file types and paths, must enforce authorization, and must remain compatible with local/offline operation before production migration. The initial implementation MUST use SQL Server LocalDB and local storage, while preserving a clean replacement path for future production storage services.
- The codebase MUST avoid production-only assumptions such as hard-coded Azure services, password hashing implementations that are not part of the training flow, or external dependencies that break offline execution.

## Development Governance

The following governance rules govern all changes:

- All work MUST remain consistent with the repository's training-first scope: local, offline, demonstrably teachable, and intentionally simplified for education.
- All security-sensitive changes MUST be reviewed for access control, data isolation, and the mock-authentication boundary before merge.
- All architectural changes that introduce infrastructure dependencies MUST identify the local implementation, the production replacement path, and the abstraction used to isolate the choice.
- The planned Document Upload and Management feature MUST follow the Spec Kit sequence of specification, clarification, plan, tasks, and MVP implementation, with each stage reviewed before the next stage begins.
- All commits and pull requests MUST describe whether the change affects training-only behavior, production migration readiness, or both.
- Any feature proposal that moves beyond the training scope must be documented as a deferred design decision or future workstream instead of being implemented opportunistically.

## Governance

This Constitution governs all work in this repository. It supersedes informal conventions and local exceptions when there is a conflict with these rules.

Amendments require a documented reason, an explicit version bump, and a review of the effect on security, architecture, and training constraints. Changes that alter the mock-authentication model, LocalDB/offline assumptions, or the repository's production-migration boundaries require special scrutiny.

The project follows semantic versioning for governance changes:
- MAJOR: backward-incompatible removal or redefinition of a governing principle;
- MINOR: addition of a principle or a material expansion of guidance;
- PATCH: clarification, wording, or non-semantic housekeeping updates.

All reviews MUST check whether the change preserves the training-only expectations, security posture, testability, and abstraction boundaries described in this Constitution. If a change cannot be validated locally, it must not be merged.

**Version**: 1.1.0 | **Ratified**: 2026-09-15 | **Last Amended**: 2026-09-15
