# Storage and Upload-Safety Contracts

## `IFileStorageService`

Required operations:

- `StageAsync(Stream content, StorageObjectDescriptor descriptor, CancellationToken)` -> opaque staging handle;
- `PromoteAsync(stagingHandle, generatedStorageReference, CancellationToken)`;
- `OpenReadAsync(generatedStorageReference, CancellationToken)` -> read stream;
- `DeleteAsync(generatedStorageReference or stagingHandle, CancellationToken)`;
- `ExistsAsync(generatedStorageReference, CancellationToken)`;
- `CleanupStagingAsync(batchId, CancellationToken)`.

The local implementation must resolve a configured absolute root outside
`wwwroot`, generate/validate GUID-based relative references, reject traversal,
create directories safely, keep staging and final files under the same volume
where possible, use async copy with the shared maximum length, and never return
a public URL. A future blob implementation preserves these semantics.

## `IUploadSafetyValidator`

`ValidateAsync(Stream or stagingHandle, UploadSafetyContext, CancellationToken)`
returns one result per file: `Passed`, `Suspicious`, or
`Unavailable/Error`. The UI and docs must say **training-only validation
simulation—not genuine malware detection**. Suspicious and unavailable/error
results reject the complete batch. A real scanner adapter is future work and
must fail closed when unavailable.

## Supported content

One shared allow-list covers PDF, DOC/DOCX, XLS/XLSX, PPT/PPTX, TXT, JPG/JPEG,
and PNG. Browser MIME claims are hints only; the service checks extension, MIME,
length, and stream readability.
