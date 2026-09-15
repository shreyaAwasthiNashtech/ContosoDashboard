# Document Service Contract

The service is the business boundary for document operations. UI components
and access endpoints must not query `ApplicationDbContext` or resolve
filesystem paths directly.

## Upload

`UploadBatchAsync(currentUser, UploadBatchRequest)` accepts one or more file
streams plus title, category, optional description/tags/project/task, and
browser metadata. It validates the whole batch before writing. The result
contains a batch ID, per-file outcomes/reasons, committed document summaries,
and a safe recoverable error when not committed.

The batch is accepted only if every file passes type, metadata, size,
project/task, authorization, and training safety validation. One failure
rejects all files.

## Reads

- `GetMyDocumentsAsync(currentUser, query)` returns only `Ready` documents
  owned by the actor.
- `SearchAsync(currentUser, query)` applies authorization before projection.
- `GetAuthorizedMetadataAsync(currentUser, documentId)` returns metadata or a
  safe not-found/forbidden result.
- `OpenContentAsync(currentUser, documentId, purpose)` authorizes immediately
  before storage access and records access/download/preview.

## Mutations

- `UpdateMetadataAsync(currentUser, documentId, metadata)`
- `ReplaceFileAsync(currentUser, documentId, replacement)`
- `DeleteAsync(currentUser, documentId)`
- `ShareAsync(currentUser, documentId, shareTarget)`
- `AttachToTaskAsync(currentUser, documentId, taskId)`

Every mutation reloads the current resource and invokes authorization.
Replacement uses the staged atomic workflow. Delete is permanent for this
release and records activity.

## Invariants

- client storage paths are never trusted;
- non-`Ready` rows are never returned;
- no document operation bypasses authorization;
- required successful/failed operations are auditable;
- `StorageReference` is generated logical metadata, not a public URL.
