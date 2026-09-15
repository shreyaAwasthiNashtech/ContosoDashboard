# Quickstart Validation Guide

This guide is for the implementation phase. It describes runnable checks and
expected outcomes; it does not implement the feature or prescribe complete
component, migration, or test-suite bodies.

## Prerequisites

- Windows workstation with .NET 10 SDK and SQL Server LocalDB
  `MSSQLLocalDB`.
- Repository root:
  `C:\Users\ShreyaAwasthi\OneDrive - NASHTECH\Documents\TrainingProjects\ContosoDashboard`
- Existing seeded users/project from
  `ContosoDashboard/Data/ApplicationDbContext.cs`.
- No internet, Azure subscription, cloud identity, or malware service.
- An isolated database created from the seeded model for schema tests; do not
  use the shared `ContosoDashboard` database for destructive test setup.

## Build and start

From the repository root:

```powershell
dotnet restore .\ContosoDashboard\ContosoDashboard.csproj
dotnet build .\ContosoDashboard\ContosoDashboard.csproj --configuration Debug
dotnet run --project .\ContosoDashboard\ContosoDashboard.csproj
```

Open the HTTPS URL printed by the app and use the existing training login.

## US1 MVP: successful upload

1. Log in as seeded employee **Ni Kang**.
2. Open Documents and select a supported PDF/text/image/Office file no larger
   than 25,000,000 bytes.
3. Enter a non-empty title and allowed category; optionally choose the seeded
   project and add description/tags.
4. Submit and observe progress/status, then success feedback.
5. Confirm the minimal My Documents list shows title, category, upload UTC,
   byte size, and project. Confirm content is not under
   `ContosoDashboard\wwwroot`.
6. Log in as an unrelated user. Confirm the document is absent and an ID/path
   request cannot download it.

Expected: one `Ready` row, successful upload activity, no public static URL,
and no unauthorized list/download visibility.

## Boundary and failure scenarios

| Scenario | Expected result |
|---|---|
| Exactly 25,000,000 bytes | Accepted if all other validation passes. |
| 25,000,001 bytes | Rejected with clear per-file size message; no row/file. |
| Unsupported extension/MIME | Rejected with clear type message; no row/file. |
| Blank title/invalid category | Rejected before storage; no row/file. |
| Multiple files, one invalid | Entire batch rejected; all staged/final artifacts cleaned. |
| Safety validator suspicious | Entire batch rejected with training-simulation wording. |
| Safety validator unavailable/throws | Fail closed with recoverable error and no visible document. |
| Metadata save/commit failure | SQL rolls back and batch artifacts are deleted. |
| `..\outside.txt` display name | Display-only metadata; generated reference remains under storage root. |

## Authorization matrix checks

For an owner, project member, project manager/team lead, administrator, and
unrelated user, test:

```text
list, search, open-by-id, preview, download, update metadata,
replace, delete, share-to-user, share-to-current-project-team,
task attachment, and activity access
```

Expected: outcomes match
[contracts/authorization.md](./contracts/authorization.md); unauthorized
operations disclose neither content nor resource existence.

## Atomicity and migration checks

Inject failures at stream copy, safety validation, staging, promotion,
metadata insert, activity insert, and commit. Verify after each failure that:

- no `Ready` row exists for any file in the batch;
- no incomplete document appears in a list;
- no staging/final file from the batch remains (or reconciliation removes it);
- the UI gives a recoverable retryable error.

For migration safety, create an isolated seeded baseline database, record
counts/IDs for Users, Projects, ProjectMembers, Tasks, Notifications, and
Announcements, remove only the document tables to simulate the pre-feature
baseline, run the actual `DocumentSchemaInitializer` twice, and verify the
legacy counts/IDs are unchanged. Only document tables, indexes, and foreign
keys should be added; the current initializer does not create EF migration
history.

## Automated validation commands

Once a test project exists, run focused tests then the solution:

```powershell
dotnet test .\tests\DocumentManagement.Tests\DocumentManagement.Tests.csproj
dotnet test
```

Required test groups cover the exact byte boundary, MIME allow-list, batch
compensation, authorization matrix, audit, offline mode, and migration
preservation.

## Plan-time limitation

No application code or test project was added by this planning workflow, so the
runtime scenarios cannot pass yet. This guide is the implementation-phase
acceptance checklist.
