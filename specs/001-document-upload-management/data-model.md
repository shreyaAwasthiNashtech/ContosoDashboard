# Data Model: Document Upload and Management

## Existing aggregates

- **User** (`UserId` int): existing identity, role, department, and
  notification preferences. Login supplies ID/role through the training
  cookie.
- **Project** (`ProjectId` int): existing project and manager.
- **ProjectMember** (`ProjectMemberId` int): existing membership and role.
- **TaskItem** (`TaskId` int): optional document association; its `ProjectId`
  is authoritative for a task attachment.
- **Notification**: existing in-app notification table for future share and
  project-document notifications.

## New entities

### Document

| Field | Type/constraints | Notes |
|---|---|---|
| `DocumentId` | `int` PK | Existing integer key convention. |
| `Title` | `nvarchar(255)` required | Trimmed; not blank. |
| `Description` | `nvarchar(2000)` nullable | Optional. |
| `Category` | `nvarchar(50)` required | Six stakeholder labels; text preserves compatibility. |
| `Tags` | `nvarchar(1000)` nullable | Bounded normalized representation. |
| `OriginalFileName` | `nvarchar(255)` required | Display only, never a path. |
| `ContentType` | `nvarchar(255)` required | Accommodates Office MIME strings. |
| `FileSizeBytes` | `bigint` required | `0 < value <= 25,000,000`. |
| `StorageReference` | `nvarchar(500)` required, unique | Generated logical reference. |
| `Status` | small/int required | `Pending`, `Ready`, or `Rejected`; only `Ready` is visible. |
| `UploadedByUserId` | `int` FK `User` required | Owner/uploader. |
| `ProjectId` | `int` FK `Project` nullable | Optional project scope. |
| `TaskId` | `int` FK `TaskItem` nullable | Optional task association. |
| `UploadedUtc` | `datetime2` required | UTC. |
| `UpdatedUtc` | `datetime2` required | UTC. |
| `SafetyValidationUtc` | `datetime2` nullable | Time of simulation result. |
| `SafetyValidationResult` | `nvarchar(32)` nullable | `Passed`, `Suspicious`, `Unavailable`. |

Validation: title/category are required; categories are Project Documents,
Team Resources, Personal Files, Reports, Presentations, Other; supported
extensions/MIME pairs are PDF, DOC/DOCX, XLS/XLSX, PPT/PPTX, TXT, JPG/JPEG,
PNG; service revalidates browser claims; each file is at most 25,000,000 bytes;
project/task references must exist; task project must match/infer the project;
only `Ready` rows can be read or changed by users.

Recommended indexes:

- `(UploadedByUserId, Status, UploadedUtc DESC)` for My Documents/recent;
- `(ProjectId, Status, UploadedUtc DESC)` for project views;
- `(Category, Status, UploadedUtc DESC)` for filtering;
- unique `StorageReference`;
- `TaskId` and `Status`/date support.

### DocumentShare

| Field | Type/constraints | Notes |
|---|---|---|
| `DocumentShareId` | `int` PK | |
| `DocumentId` | `int` FK required | No dangling active share. |
| `RecipientUserId` | `int` FK nullable | Individual mode. |
| `ProjectId` | `int` FK nullable | Current-project-team mode; equals document project. |
| `SharedByUserId` | `int` FK required | Actor. |
| `SharedUtc` | `datetime2` required | |
| `RevokedUtc` | `datetime2` nullable | Active query requires null. |
| `NotificationCreated` | `bit` required | Avoids duplicate notification work. |

Exactly one recipient mode is required. An active unique index prevents
duplicate document/user or document/project-team grants.

### DocumentActivity

| Field | Type/constraints | Notes |
|---|---|---|
| `DocumentActivityId` | `bigint` PK | High-volume audit key. |
| `DocumentId` | `int` FK nullable | Nullable for failed/deleted operations if retained. |
| `ActorUserId` | `int` FK required | Initiating user. |
| `ActivityType` | `nvarchar(32)` required | Upload, Access, Download, Replace, UpdateMetadata, Delete, Share, Preview, Reject. |
| `OccurredUtc` | `datetime2` required | UTC. |
| `Success` | `bit` required | Outcome. |
| `Reason` | `nvarchar(500)` nullable | Safe diagnostic only. |
| `BatchId` | `uniqueidentifier` nullable | Multi-file correlation. |

Recommended indexes: `(DocumentId, OccurredUtc DESC)`, `(ActorUserId,
OccurredUtc DESC)`, `(ActivityType, OccurredUtc DESC)`, and `BatchId`.

## Upload states

```text
not-created
  ├─ validation/safety failure ──────> rejected/no visible row
  └─ staged + Pending metadata
          ├─ storage/persistence failure -> rollback + cleanup
          └─ all promoted + commit -> Ready/visible
```

`Pending` is an internal crash/compensation guard. User queries always filter
it out. Reconciliation may delete stale pending rows and generated references
after confirming no active batch owns them.

## Access rules

1. Administrator: all `Ready` documents.
2. Owner: own `Ready` documents.
3. Project member: project documents for current membership.
4. Project manager/authorized project lead: manage documents in that project.
5. Share recipient: active individual or current-project-team grant, subject
   to the project still being accessible.

Task attachment requires access to both task/project and document. An ID/path
lookup must return a safe not-found/forbidden result without disclosing an
unauthorized resource.

## Migration and seed safety

The three new tables and indexes/FKs are additive. Existing Users, Projects,
ProjectMembers, Tasks, Notifications, Announcements, and their seed rows must
not be altered. See the guarded baseline procedure in [plan.md](./plan.md).
