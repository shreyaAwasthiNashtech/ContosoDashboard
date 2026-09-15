# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-15 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

## Summary

Add an offline-first document workflow to the existing .NET 10 Blazor Server
application. The independently testable US1 MVP is an authenticated upload
flow with supported-file validation, an exact 25,000,000-byte per-file limit,
training-only malware-validation simulation, progress/status feedback, atomic
multi-file commit/rollback, and a minimal authorized **My Documents** list.
The design also establishes the contracts for browsing, preview/download,
replacement, sharing, task/dashboard integration, and audit reporting.

Files are stored outside `wwwroot` by an `IFileStorageService`; metadata is
stored in SQL Server LocalDB through EF Core. Every document operation performs
service-layer resource authorization in addition to page/endpoint authorization.
The existing cookie-based mock authentication remains explicitly training-only.
Database evolution uses additive EF migrations and a guarded baseline bootstrap
so an already created/seeded LocalDB is never dropped or overwritten.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`), nullable reference types enabled  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10 SQL Server provider, existing cookie authentication and DI services  
**Storage**: SQL Server LocalDB/MSSQLLocalDB for metadata; local filesystem under a configured application-data root outside `wwwroot` for content  
**Testing**: Focused unit tests, EF Core SQL Server/LocalDB integration tests, and Blazor/manual acceptance scenarios in [quickstart.md](./quickstart.md)  
**Target Platform**: Offline Windows training workstation running the existing Blazor Server application  
**Project Type**: Single ASP.NET Core web project with Razor/Blazor pages and layered `Models`, `Data`, `Services`, and `Pages` directories  
**Performance Goals**: Upload up to 25,000,000 bytes within 30 seconds; authorized list/search of up to 500 documents within 2 seconds; PDF/image preview within 3 seconds  
**Constraints**: No cloud services or external identity/scanning service; generated non-public paths; per-file validation; one failed file rejects the batch; no visible incomplete records; every protected operation is independently authorized and audited  
**Scale/Scope**: Existing seeded users/projects; up to 500 authorized documents per list/search scenario; six fixed categories and PDF/Office/text/JPEG/PNG allow-list

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Result | Evidence/plan |
|---|---|---|
| Quality before speed | PASS | Business rules live in services; Razor pages orchestrate input and display outcomes. |
| Security by default | PASS | Every list, search, preview, download, update, replace, delete, share, and task-attachment operation invokes resource authorization; files are never directly public. |
| Test and validate changes | PASS | Focused tests cover type/size validation, authorization, rollback, database behavior, audit, and offline operation. |
| Maintainable boundaries | PASS | Models, EF data access, document service, authorization evaluator, safety validator, and replaceable storage interfaces are separated and DI-registered. |
| Offline-first constraints | PASS | LocalDB, local filesystem, cookie mock auth, and deterministic local safety simulation are the defaults. |
| Production migration readiness | PASS | `IFileStorageService` and `IUploadSafetyValidator` support future blob storage and genuine scanning adapters. |
| Workflow governance | PASS | This workflow changes planning artifacts only; implementation is deferred. |

No gate violation or unresolved clarification remains. The required decisions
are recorded in [research.md](./research.md).

## Architecture and implementation direction

### Existing boundaries to preserve

```text
ContosoDashboard/
├── Program.cs                         # DI, cookie auth, policies, current EnsureCreated
├── ContosoDashboard.csproj            # net10.0 and EF Core 10 SQL Server packages
├── Data/ApplicationDbContext.cs       # existing seeded Users/Projects/Tasks/etc.
├── Models/                             # User, Project, ProjectMember, TaskItem, ...
├── Services/                           # existing service interfaces/implementations
├── Pages/                              # Blazor pages plus Login/Logout Razor Pages
└── Shared/
```

Add document models and EF configuration under `Models`/`Data`, document and
infrastructure services under `Services`, authenticated Blazor UI under
`Pages`, and focused tests in the repository's chosen test project(s).

### MVP upload workflow

1. Require an authenticated user; capture title, category, optional
   description/tags/project/task, and one or more files.
2. Resolve the user from the existing `NameIdentifier` cookie claim. Validate
   all metadata, project/task membership, allowed MIME/extension pairs, and
   `Length <= 25_000_000` before writing anything.
3. Run `IUploadSafetyValidator`. The offline result is explicitly labelled
   **training-only simulation, not genuine malware detection**. Suspicious or
   unavailable/error results reject the complete batch; real anti-malware is
   future work.
4. Generate all logical references and stage all streams below a configured
   application-data root outside `wwwroot`; never use a user filename as a
   path.
5. In one EF transaction, insert `Pending` rows, promote staged files to
   generated final paths on the same volume, mark every row `Ready`, append
   upload activity, and commit. Any validation, storage, promotion,
   persistence, or commit failure rolls back SQL and deletes every staged or
   final artifact created by the batch.
6. All queries filter to `Ready`; the UI shows per-file reasons plus a
   recoverable error or success and refreshes the authorized My Documents list.

This is a compensating transaction across SQL Server and the filesystem, not a
distributed transaction. A reconciliation check may remove abandoned staging
files and stale non-ready rows after a process crash; it must never expose them.

### Authorization direction

Use imperative/resource-based checks in one
`IDocumentAuthorizationService`. Broad page/endpoint policies establish
authentication and roles; the document service performs the definitive check
for list, search, preview, download, metadata update, replacement, delete,
share, task attachment, and activity retrieval. Queries apply the authorized
predicate before projection. Missing or stale project/user records deny access.

### Safe additive schema update

`ApplicationDbContext` currently uses `EnsureCreated`, and seeded LocalDB
databases may have data without `__EFMigrationsHistory`. Do not drop/recreate
or apply a generated all-table initial migration directly to such a database.

1. Capture a reviewed baseline migration/snapshot and add a separate
   `DocumentSchema` migration whose `Up` creates only document tables, indexes,
   and foreign keys to existing `Users`, `Projects`, and `Tasks`.
2. Add a guarded initializer. On an existing database with expected legacy
   tables but no migration history, create the history table if needed, mark
   the reviewed baseline as applied without replaying legacy DDL, then apply
   pending document migrations. A fresh database runs the full chain.
3. Replace unconditional `EnsureCreated` only after scripts are reviewed.
   Fail closed if the expected legacy shape is absent; never guess or delete.
4. Test against a backup/copy of seeded LocalDB: legacy row counts and seed
   IDs remain unchanged; only document tables/history/indexes/FKs are added.

An idempotent SQL script generated from the reviewed migration is the fallback
for training machines without EF CLI.

## Constitution Check (post-design)

| Gate | Result | Post-design evidence |
|---|---|---|
| Security by default | PASS | No direct file URL; generated paths; service authorization and authorization-filtered queries; audit on access-changing operations. |
| Test and validate changes | PASS | [quickstart.md](./quickstart.md) covers exact boundaries, failure injection, role matrix, audit, offline LocalDB, and migration preservation. |
| Offline-first | PASS | Default implementations are local and deterministic; external scanner/blob adapters are future replacements. |
| Maintainability | PASS | Contracts isolate UI, orchestration, storage, validation, authorization, notifications, and EF persistence. |
| Governance | PASS | No application source code was changed. |

## Project Structure

### Documentation

```text
specs/001-document-upload-management/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── document-service.md
│   ├── storage-and-safety.md
│   ├── authorization.md
│   └── ui.md
└── tasks.md                         # created later by /speckit.tasks
```

### Source code (planned; not implemented by this workflow)

```text
ContosoDashboard/
├── Models/Document.cs, DocumentShare.cs, DocumentActivity.cs
├── Data/ApplicationDbContext.cs, Migrations/
├── Services/DocumentService.cs, DocumentAuthorizationService.cs
│   ├── LocalFileStorageService.cs, TrainingUploadSafetyValidator.cs
│   └── DocumentActivityService.cs
├── Pages/Documents.razor, DocumentAccess endpoint/component
└── Program.cs                         # DI and migration-aware initialization
```

**Structure Decision**: Keep the current single Blazor Server project and its
Models/Data/Services/Pages separation. Do not add a second web project,
public storage directory, or cloud dependency.

## Validation plan

The implementation phase must run [quickstart.md](./quickstart.md) and record
automated results. At minimum it must prove:

- 25,000,000 bytes succeeds and 25,000,001 bytes fails per file;
- one invalid file rejects an otherwise valid batch and cleans all artifacts;
- safety simulation suspicious/unavailable results fail closed;
- unauthorized users cannot list/search/open/download/update/delete/share by
  identifier or path;
- seeded LocalDB rows survive additive migration bootstrap;
- only `Ready` rows appear in My Documents and activities are auditable.

## Complexity Tracking

| Addition | Why needed | Simpler alternative rejected because |
|---|---|---|
| Pending/Ready state plus filesystem compensation | SQL Server and the filesystem cannot share a local distributed transaction; visibility must remain atomic for a batch. | Direct final-path writes can leave orphaned files or incomplete records. |
| Guarded migration baseline bootstrap | Existing databases were created with `EnsureCreated` and may lack migration history. | Drop/recreate or all-table initial migration would lose/collide with existing data. |

## Generated artifacts

- [research.md](./research.md)
- [data-model.md](./data-model.md)
- [quickstart.md](./quickstart.md)
- [contracts/](./contracts/)
