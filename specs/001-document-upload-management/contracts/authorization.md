# Document Authorization Contract

`IDocumentAuthorizationService` is called by every document service operation
after authentication and before data is returned or changed.

## Decisions

- Unauthenticated actors are denied.
- Administrators manage all `Ready` documents.
- Owners read, update metadata, replace, delete, and share their own files.
- Project members read/download/preview project documents for current
  membership.
- Project managers and authorized project leads/team leads manage project
  documents within their project.
- Individual shares grant read/download/preview only to the active recipient.
- Team shares grant read/download/preview only to current project-team members.
- Share creation is restricted to owner, administrator, or authorized project
  manager/lead and verifies the target.
- Task attachment requires access to both task/project and document.
- Deleted/inaccessible projects deny access even if a stale share exists.

## Required checks

The evaluator exposes operation-specific checks such as:

```text
CanList(actor)
CanRead(actor, document)
CanManageMetadata(actor, document)
CanReplace(actor, document)
CanDelete(actor, document)
CanShare(actor, document, target)
CanAttachToTask(actor, document, task)
CanViewAudit(actor, document/activity)
```

Queries apply the equivalent authorization predicate before projection. An
ID/path lookup must not disclose whether an unauthorized resource exists.
