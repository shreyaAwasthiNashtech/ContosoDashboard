# Research: Document Upload and Management

## Scope and repository findings

The feature specification and stakeholder document were reviewed against the
repository on 2026-09-15. The application is a single `net10.0` ASP.NET Core
project. `ContosoDashboard/Program.cs` registers Blazor Server, EF Core SQL
Server, cookie authentication, role policies, and existing services. The
cookie is created by `ContosoDashboard/Pages/Login.cshtml.cs` with
`NameIdentifier`, `Name`, `Email`, and `Role` claims.
`ApplicationDbContext` currently uses `EnsureCreated()` and model seeding for
users, projects, project members, tasks, notifications, and announcements.
There is no `Migrations` directory or test project in the current tree.

The requested `.specify/templates/commands/plan.md` path is absent in this
checkout. The generated `.specify/templates/plan-template.md` was used
instead. This is a planning-workflow limitation, not an application-source
change.

## Decision 1: Training-only safety validation, not genuine malware detection

**Decision**: Introduce `IUploadSafetyValidator` with a deterministic local
training implementation. It is labelled **simulation, not genuine malware
detection**. It returns `Passed`, `Suspicious`, or `Unavailable/Error` for
each staged file. Suspicious and unavailable/error results reject the complete
batch before any document becomes visible. Genuine anti-malware integration
remains future work behind the same interface.

**Rationale**: The application must work offline and has no scanner service.
Fail-closed behavior avoids making an unvalidated file available while keeping
the boundary replaceable for production.

**Alternatives rejected**:

- Treating all files as clean falsely implies security.
- Calling a cloud scanner breaks offline-first operation.
- Showing a quarantined file in normal lists lets users mistake it for an
  available document; a future quarantine workflow can reuse the status model.

## Decision 2: Authorization on every document operation

**Decision**: Use a service-level `IDocumentAuthorizationService` and
imperative/resource-based checks. Broad `[Authorize]`/policy checks protect
the page or endpoint, but the document service performs the definitive check
for list, search, preview, download, metadata update, replacement, delete,
share, task attachment, and activity retrieval. List/search queries apply the
authorized predicate before projection.

**Permission decision**:

| Operation | Employee/member | Team lead/project manager | Administrator |
|---|---|---|---|
| Own personal document | read/manage own | read/manage own | all |
| Project document | read if current member | read/manage within authorized project | all |
| Share to selected user | own only | own or authorized project document | all |
| Share to current project team | own/PM/authorized lead | within authorized project | all |
| Delete | own | own or authorized project document | all |
| Audit reporting | no | no | yes |

Missing or inaccessible projects deny access. An ID/path lookup must not
disclose whether an unauthorized resource exists.

**Alternatives rejected**: role attributes alone cannot inspect ownership or
project membership; generated paths are not authorization; checking only
downloads leaves list/search/update/delete/share vulnerabilities.

## Decision 3: Exact decimal per-file limit

**Decision**: Define one shared constant `MaxFileBytes = 25_000_000`. Validate
each selected file before staging. `25,000,000` is accepted and `25,000,001`
is rejected. There is no combined batch limit.

**Rationale**: This exactly follows FR-003 and prevents inconsistent UI,
Blazor stream, and service limits. The service remains authoritative.

**Alternatives rejected**: binary 25 MiB conflicts with the explicit decimal
requirement; a combined batch limit conflicts with the clarification.

## Decision 4: Atomic multi-file upload with compensation

**Decision**: Use a staged-file plus SQL transaction workflow:

1. Validate all metadata, authorization, type/size, and safety results first.
2. Generate all final references before database insert; write streams to a
   staging directory outside `wwwroot`.
3. Begin one EF transaction and add all metadata as `Pending`.
4. Promote all staged files to generated final paths on the same volume.
5. Mark all rows `Ready`, add upload activity, and commit.
6. On any exception (validation, stream copy, promotion, metadata save, or
   commit), roll back SQL and delete every staging/final path created by the
   batch. Return per-file reasons and a recoverable failure.

Queries include `Status == Ready`; pending or rejected rows are never visible.
A startup reconciler removes abandoned staging files and non-ready rows after
a crash. This is a compensating transaction, not a distributed transaction.

**Alternatives rejected**: metadata-first leaves orphan records, file-first
leaves orphan content, and a distributed transaction is unnecessary and not
available for a local filesystem in the offline target.

## Decision 5: Replaceable local storage boundary

**Decision**: `IFileStorageService` stages/promotes/deletes/opens content by an
opaque generated logical reference, never a physical path. The local
implementation resolves a configured absolute root such as `AppData/uploads`
(outside `wwwroot`), verifies every resolved path remains under that root,
creates directories safely, and uses GUID-based names. A future blob adapter
preserves the logical-reference contract.

**Alternatives rejected**: SQL BLOBs change the requested local-file pattern;
`wwwroot` enables static bypass; original names introduce traversal/collision
risk.

## Decision 6: Additive EF schema update for existing LocalDB

**Decision**: Do not drop/recreate or directly apply a generated all-table
initial migration to a database made by `EnsureCreated`. Capture a reviewed
baseline migration/snapshot, add a separate document-only migration, and use a
guarded initializer:

- Fresh database: apply the complete migration chain.
- Existing database with legacy tables and no migration history: create/seed
  the reviewed baseline migration marker without replaying legacy table DDL,
  then apply only pending document migrations.
- Unexpected/missing legacy shape: stop and log an actionable error rather
  than guessing or deleting data.

Generate and review an idempotent SQL script for environments without EF CLI.
Back up/copy seeded LocalDB before testing and verify all legacy row counts and
seed IDs afterward.

**Alternatives rejected**: continuing `EnsureCreated` does not evolve schema;
drop/recreate loses data; a full initial migration collides with existing
tables; untracked ad-hoc `CREATE TABLE` calls cause schema drift.

## Decision 7: MVP UI slice

**Decision**: Implement US1 independently before P1/P2 expansion: one
authenticated Blazor page/modal with required title/category, optional
metadata, multi-file selection, progress/status/error messages, and a minimal
My Documents table. The list is authorized and only shows `Ready` rows. Later
stories reuse the same service contracts.

**Rationale**: US1 proves security, storage, metadata, rollback, and feedback
without requiring every secondary workflow.

## References

- Repository: `ContosoDashboard/Program.cs`,
  `ContosoDashboard/Data/ApplicationDbContext.cs`,
  `ContosoDashboard/Pages/Login.cshtml.cs`, existing `Services/*`, and
  `ContosoDashboard/ContosoDashboard.csproj`.
- Feature inputs: `spec.md` and
  `StakeholderDocs/document-upload-and-management-feature.md`.
- EF Core migrations:
  https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- EF Core transactions:
  https://learn.microsoft.com/en-us/ef/core/saving/transactions
- Blazor file uploads:
  https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0
- Resource-based authorization:
  https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0
