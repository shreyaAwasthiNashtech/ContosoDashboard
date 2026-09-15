# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/`

**Goal**: Decompose the approved document-upload feature into independent, testable implementation phases while preserving the seeded LocalDB baseline and keeping the US1 MVP first.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the repository constraints, the LocalDB baseline, and the implementation boundaries before any document feature work begins.

- [x] T001 [P] Confirm the feature scope, requirement IDs, and contract references in `specs/001-document-upload-management/spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/` and record the exact FR and contract mapping used by the implementation phase.
- [x] T002 [P] Establish the LocalDB preservation plan and migration safety checklist in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Program.cs` so the existing seeded LocalDB is not dropped or overwritten during additive document schema updates; capture backup/count checks for legacy seed IDs and rows before migration validation (Decision 6, FR-022, quickstart.md: "Atomicity and migration checks").
- [x] T003 [P] Set up the working implementation structure and testing boundaries for `ContosoDashboard/Models/`, `Data/`, `Services/`, `Pages/`, and `tests/` without creating application logic before the approved design is implemented (plan.md: source-code structure, constitution governance).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the infrastructure contracts that all document stories depend on, especially authorization, storage, safety validation, migration safety, and the batch upload orchestration contract.

**Checkpoint**: Foundation ready - user story implementation may begin after all tasks in this phase pass their review gates.

- [x] T010 Implement the `Document`, `DocumentShare`, and `DocumentActivity` entities and EF configuration in `ContosoDashboard/Models/` and `ContosoDashboard/Data/ApplicationDbContext.cs`, including required constraints from `data-model.md` (`Title`, `Category`, `FileSizeBytes`, `StorageReference`, `Status`, `UserId`, `ProjectId`, `TaskId`, and audit fields) and the `Ready`/`Pending`/`Rejected` visibility rules. This foundation task owns the model implementation required by the migration (FR-006, FR-020, data-model.md).
- [x] T011 [P] Depends on T010. Implement the guarded additive schema bootstrap in `ContosoDashboard/Data/DocumentSchemaInitializer.cs` and `ContosoDashboard/Program.cs` so existing seeded LocalDB data is preserved while document tables, indexes, and FKs are added without dropping or replaying legacy schema; verify the existing seeded dashboard/project data remains available after startup (Decision 6, research.md, quickstart.md: "Atomicity and migration checks").
- [x] T012 [P] Define the `IUploadSafetyValidator` contract and training-only validation boundary in `ContosoDashboard/Services/` so the UI and service wording explicitly say this is a simulation, not genuine malware detection, and so suspicious/unavailable validation results fail closed before any document is visible (FR-007, research.md: Decision 1, contracts/storage-and-safety.md: `IUploadSafetyValidator`, quickstart.md: "Boundary and failure scenarios").
- [x] T013 [P] Define the `IFileStorageService` contract and local storage root rules in `ContosoDashboard/Services/` to guarantee generated references outside `wwwroot`, safe directory creation, no public URL exposure, and cleanup of staged/final artifacts during compensation (FR-008, research.md: Decision 5, contracts/storage-and-safety.md: `IFileStorageService`).
- [x] T014 Define the `IDocumentAuthorizationService` operation matrix and all authorization checks in `ContosoDashboard/Services/` so list/search/open/preview/download/update/replace/delete/share/task-attach/audit operations are validated before data is returned or changed (FR-021, research.md: Decision 2, contracts/authorization.md: required checks).
- [x] T015 Define the document upload orchestration service contract in `ContosoDashboard/Services/DocumentService.cs` for batched validation, transactional commit, compensating cleanup, per-file failure reasons, and safe recoverable error handling for single-file and multi-file submissions (FR-001, FR-009, FR-023, contracts/document-service.md: Upload and Invariants).
- [x] T016 [P] Create and run the foundation test suite in `tests/DocumentManagement.Tests/` for exact byte boundary checks, authorization matrix checks for the implemented MVP operations, multi-file rollback cleanup, additive initializer preservation, and training-only safety simulation wording (FR-003, FR-007, FR-009, FR-021, quickstart.md: automated validation commands).

---

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) 🎯 MVP

**Goal**: Deliver the independently testable insert/upload MVP: authenticated upload, exact 25,000,000-byte validation, training-only safety simulation, per-file status feedback, atomic batch rollback, and a minimal authorized My Documents list.

**Independent Test**: Log in as an authenticated employee, upload one or more supported files, verify upload progress/success feedback, confirm a minimal My Documents list shows the uploaded item, verify unauthorized users cannot see/download it, and confirm one bad file rejects the whole batch without leaving rows or files behind.

### Tests for User Story 1

- [x] T020 [P] [US1] Add US1 upload success test in `tests/DocumentManagement.Tests/UploadTests.cs` covering a supported PDF/text/image/Office file at exactly `25,000,000` bytes, success feedback, and the minimal My Documents list showing title, category, upload UTC, file size, and project (FR-003, FR-010, FR-023, quickstart.md: "US1 MVP: successful upload").
- [x] T021 [P] [US1] Add US1 rejection tests in `tests/DocumentManagement.Tests/UploadTests.cs` for unsupported type, blank title or invalid category, `25,000,001` bytes, suspicious safety result, unavailable safety result, and multi-file atomic rollback with no partial commit (FR-003, FR-004, FR-007, FR-009, quickstart.md: "Boundary and failure scenarios").
- [x] T022 [P] [US1] Add US1 authorization boundary tests in `tests/DocumentManagement.Tests/AuthorizationTests.cs` proving the uploader sees the document in My Documents, unrelated users cannot list or open it by ID/path, and unauthorized download requests are denied by the implemented MVP download boundary rather than treated as a 404-only proxy (FR-010, FR-013, FR-021, contracts/authorization.md).

### Implementation for User Story 1

- [x] T023 [US1] Implement the authorized minimal My Documents query/projection in `ContosoDashboard/Services/DocumentService.cs`, returning only `Ready` documents owned by or authorized for the current user with title, category, upload UTC, file size, and project information (FR-010, FR-021, contracts/document-service.md, contracts/authorization.md).
- [x] T024 [US1] Implement batch upload validation and safety orchestration in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/TrainingUploadSafetyValidator.cs` to enforce the exact `25,000,000`-byte per-file limit, supported MIME/extension allow-list, metadata rules, project/task checks, and training-only simulation language before any row becomes visible; reject suspicious or unavailable safety results as fail-closed (FR-003, FR-004, FR-006, FR-007, FR-021, contracts/storage-and-safety.md, contracts/document-service.md).
- [x] T025 [US1] Implement atomic staging, promotion, and rollback cleanup in `ContosoDashboard/Services/LocalFileStorageService.cs` and `ContosoDashboard/Services/DocumentService.cs` so every file in a multi-file batch is staged and then either committed together or fully cleaned up on any validation, storage, promotion, persistence, or commit failure; ensure all artifacts are outside `wwwroot` and no direct path is exposed (FR-008, FR-009, research.md: Decision 4, contracts/storage-and-safety.md).
- [x] T026 [US1] Implement the authenticated upload page and minimal My Documents list in `ContosoDashboard/Pages/Documents.razor` and related components to provide required title/category fields, optional description/tags/project/task metadata, multi-file input, progress/idle/validating/uploading/commit success/error states, per-file messages, and a minimal table showing title, category, upload date, file size, and project (FR-010, FR-023, contracts/ui.md: "MVP Documents page").
- [x] T027 [US1] Connect the document service to the UI and authorization layer, including the minimal authorized download/content boundary required by T022, so My Documents filters to authorized `Ready` rows only, records upload activity, rejects unauthorized view/download requests without disclosing resource content, and refreshes the list only after a committed result (FR-010, FR-013, FR-020, FR-021, contracts/ui.md, contracts/authorization.md).

**US1 MVP task IDs**: `T020`, `T021`, `T022`, `T023`, `T024`, `T025`, `T026`, `T027`

**US1 dependency chain for independent testability**:
- Complete setup `T001`-`T003` first.
- Foundation gates `T010`-`T016` must complete before US1 implementation begins; `T010` precedes `T011`.
- Service/model dependencies: `T023` depends on `T010`, `T011`, and `T014`; `T024` and `T025` depend on `T010`, `T011`, `T012`, `T013`, `T014`, and `T015`; `T026` depends on `T023`-`T025`; `T027` depends on `T023`-`T026`.
- Tests `T020`-`T022` may be authored after foundation, but must execute after their implemented boundaries exist; `T022` executes after `T027` so its download authorization assertion exercises the real MVP boundary.
- MVP validation tasks `T100` and `T101` must pass after `T020`-`T027`; no US2-US5 task is required for this gate.

**US1 acceptance criteria**:
- Authenticated employee uploads a supported file and receives explicit upload feedback.
- The file is stored outside `wwwroot` under a generated logical reference.
- The exact 25,000,000-byte per-file limit is enforced and `25,000,001` is rejected.
- The training-only safety result is explicitly labeled as simulation, not genuine malware detection, and suspicious or unavailable results fail closed.
- A bad file in a multi-file upload rejects the batch, cleans staged/final files, and leaves no visible document.
- Unauthorized users cannot list, open, or download the uploaded document.

---

## Phase 4: User Story 2 - Browse and Find Authorized Documents (Priority: P1)

**Goal**: Deliver browsing, filtering, sorting, and search for the documents an actor is actually allowed to access.

**Independent Test**: Seed authorized and unauthorized documents, then verify an authenticated employee gets the correct filtered results and unauthorized documents are hidden.

- [ ] T030 [P] [US2] Implement authorized retrieval and search logic in `ContosoDashboard/Services/DocumentService.cs` for sorting by title, upload date, category, and size; filtering by category, project, and date range; and searching title, description, tags, uploader, and project while applying the authorization predicate before projection (FR-011, FR-012, contracts/document-service.md: Reads).
- [ ] T031 [US2] Add project documents and search UI in `ContosoDashboard/Pages/` and shared components to render authorized results, project filters, and the minimal search experience without exposing unauthorized documents (FR-010, FR-011, FR-012, contracts/ui.md: Later UI contracts).
- [ ] T032 [P] [US2] Add US2 tests in `tests/DocumentManagement.Tests/SearchTests.cs` covering authorized sorts/filters/searches and exclusion of unauthorized documents (FR-011, FR-012, contracts/authorization.md).

---

## Phase 5: User Story 3 - Download, Preview, and Manage Documents (Priority: P1)

**Goal**: Allow authorized users to preview/download, update metadata, replace the file, and delete documents without bypassing access checks.

**Independent Test**: Exercise preview/download, metadata edit, replace, and delete as owner, project manager, administrator, and unrelated user; verify each permission boundary and audit trail.

- [ ] T040 [P] [US3] Implement read/preview/download and metadata update/replace/delete operations in `ContosoDashboard/Services/DocumentService.cs` with resource authorization, file-read-through abstraction, and audit logging for access-changing actions (FR-013, FR-014, FR-015, FR-020, contracts/document-service.md: Reads and Mutations, contracts/authorization.md).
- [ ] T041 [US3] Implement the authenticated preview/download endpoint and management UI in `ContosoDashboard/Pages/` and any server-side streaming boundary to resolve cookie identity, authorize access, stream content safely, and record preview/download/access activity without exposing physical storage paths (FR-013, FR-020, contracts/ui.md: "Authorized content endpoint").
- [ ] T042 [P] [US3] Add US3 authorization and operation tests in `tests/DocumentManagement.Tests/DocumentLifecycleTests.cs` for owner, project manager/team lead, administrator, and unrelated user scenarios across preview/download/update/replace/delete and confirm no unauthorized resource disclosure (FR-013, FR-014, FR-015, FR-021, contracts/authorization.md).

---

## Phase 6: User Story 4 - Share Documents and Receive Notifications (Priority: P2)

**Goal**: Allow controlled sharing and notifications without violating recipient or project boundaries.

**Independent Test**: Share a document to a user and a project team, then verify only the intended recipients see it in Shared with Me and receive notifications.

- [ ] T050 [P] [US4] Implement share validation and permission rules in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/DocumentShare.cs` for individual-user and current-project-team grants, active-share enforcement, and the owner/manager/lead constraints described in `contracts/authorization.md` (FR-016, FR-021).
- [ ] T051 [US4] Implement notification creation and recipient visibility in `ContosoDashboard/Services/` and `ContosoDashboard/Pages/` so document shares and project document additions notify only authorized recipients (FR-016, FR-017, data-model.md: `DocumentShare` and existing `Notification` model).
- [ ] T052 [P] [US4] Add US4 share and notification tests in `tests/DocumentManagement.Tests/ShareTests.cs` confirming authorized recipients can find shared documents and non-recipients cannot access them (FR-016, FR-017, contracts/authorization.md).

---

## Phase 7: User Story 5 - Use Documents from Tasks and the Dashboard (Priority: P2)

**Goal**: Connect documents to tasks and the dashboard without breaking the document authorization rules.

**Independent Test**: Upload or attach a document to a task, verify the project/task association, and confirm the dashboard shows the current user's five most recent documents and a document count summary.

- [ ] T060 [P] [US5] Implement task attachment and dashboard summary logic in `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/TaskDocumentService.cs` (or equivalent), and related data access to associate documents with the task and its project and to compute the current user's recent-five/count display (FR-018, FR-019, spec.md: US5 acceptance scenarios).
- [ ] T061 [US5] Add task and dashboard UI integration in `ContosoDashboard/Pages/` and `Shared/` to show the five most recent documents and the document count for the current user while keeping the results authorized and filtered to `Ready` rows (FR-019, contracts/ui.md: Later UI contracts).
- [ ] T062 [P] [US5] Add US5 integration tests covering task attachment, project association, and dashboard summary results (FR-018, FR-019, spec.md: US5 acceptance scenarios).

---

## Phase 8: MVP Validation and Later-Story Validation

**Purpose**: Final validation of migration safety, manual acceptance coverage, and implementation readiness for the MVP-first release.

- [x] T100 [P] [MVP] Execute the actual guarded `DocumentSchemaInitializer` used by `ContosoDashboard/Program.cs` against an isolated seeded baseline, confirming seeded LocalDB row counts and IDs remain unchanged while only document tables, indexes, and foreign keys are added; do not claim historical before/after preservation for the shared database and do not use `ContosoDashboard/Data/Migrations/` because no EF migrations are implemented (Decision 6, FR-022, quickstart.md: "Atomicity and migration checks").
- [ ] T101 [P] [MVP] Run and record the US1 MVP manual acceptance checklist from `specs/001-document-upload-management/quickstart.md`: setup `T001`-`T003`, foundation `T010`-`T016`, US1 implementation/tests `T020`-`T027`, upload feedback, minimal My Documents list, exact byte boundary, training-only safety wording, authorization including the real download boundary, atomic multi-file rollback, and migration preservation from T100; this gate must not require US2-US5 (FR-003, FR-007, FR-009, FR-013, FR-021, FR-023).
- [ ] T102 [P] [LATER] After the MVP gate passes, run the remaining US2-US5 automated and manual validation scenarios from the quickstart, including search, lifecycle, sharing, task integration, dashboard integration, and their authorization matrices; this task is not part of the MVP completion gate.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (Setup)**: No dependencies; begins immediately.
- **Phase 2 (Foundation)**: Depends on Setup completion; blocks all user-story work.
- **Phase 3 (US1 MVP)**: Depends on the Foundation phase and is intentionally the first user-story milestone. It must pass before broader stories proceed.
- **Phase 4/5/6/7 (US2–US5)**: Depend on the Foundation phase and should proceed once US1 is independently validated; they can be parallelized after the MVP gate.
- **Phase 8 MVP validation**: T100 and T101 depend only on setup `T001`-`T003`, foundation `T010`-`T016`, and US1 `T020`-`T027`; they do not depend on US2-US5.
- **Later-story validation**: T102 depends on the completion of the applicable US2-US5 tasks and is outside the MVP gate.

### User story dependencies

- **US1**: `T010` → `T011` → `T012` → `T013` → `T014` → `T015` → `T016` → (`T023`, `T024`, `T025`, `T026`, `T027`)
- **US2**: depends on US1 or at minimum the same core authorization/storage foundation plus the document model and search service contracts; can proceed after US1 passes the independent test gate.
- **US3**: depends on US1 plus the read/update/delete authorization and storage abstractions.
- **US4**: depends on US1 and share authorization contracts.
- **US5**: depends on US1 plus task/project association rules.

### Parallel opportunities

- `T001`, `T002`, and `T003` can start in parallel during setup.
- `T010`, `T012`, `T013`, `T014`, and `T015` can be implemented in parallel after setup; `T011` starts after T010.
- Within US1, the tests `T020`, `T021`, and `T022` can run in parallel and are independent of the service/UI implementation work once the foundation is ready.
- `T030`, `T032`, `T040`, `T042`, `T050`, `T052`, `T060`, and `T062` are parallelizable once the core document contracts exist.

---

## Acceptance Criteria Summary

- The seeded LocalDB database remains intact during additive migration setup and document schema addition (`T002`, `T011`, `T100`).
- Every document operation is subject to authorization tests (`T014`, `T016`, `T022`, `T042`, `T052`).
- Off-line malware simulation is clearly labeled as training-only and not genuine malware detection (`T012`, `T024`, `T101`).
- The per-file limit is exactly `25,000,000` bytes, with `25,000,001` rejected (`T020`, `T021`, `T024`, `T101`).
- A failed multi-file upload rolls back the full batch and deletes all staged/final artifacts (`T015`, `T021`, `T025`, `T101`).
- Manual acceptance testing confirms the US1 MVP and later scenarios match the documented quickstart (`T101`).
- The complete US1 MVP scope is setup `T001`-`T003`, foundation `T010`-`T016`, US1 `T020`-`T027`, and MVP validation `T100`-`T101`; later-story validation `T102` is excluded.

## Implementation Strategy

### MVP first (User Story 1 only)

1. Complete Phase 1 and Phase 2 foundation tasks.
2. Deliver `T020`-`T027` as the minimal upload flow for authenticated employees.
3. Validate the US1 independent test before starting US2-US5.
4. Expand to browse/search/preview/manage/share/task integration only after the upload and authorization contracts have stabilized.

### Repository-specific notes

- This task list intentionally does not create application code; it defines the implementation gates and acceptance criteria for the planned service/UI work.
- The design will remain compatible with the existing Blazor Server + LocalDB architecture and the repository constitution.
- No unresolved placeholders remain in this tasks artifact: all requirement IDs, contract references, and user stories are mapped to concrete tasks.
