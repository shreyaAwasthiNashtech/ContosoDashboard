# Blazor UI Contract

## MVP Documents page

The authenticated page provides required title/category fields, optional
description/tags/project/task fields, multi-file `InputFile` selection with
the 25,000,000-byte per-file limit, progress/status states (idle, validating,
uploading, committing, success, recoverable error), per-file messages, and
explicit training-simulation safety wording. The minimal My Documents list
shows title, category, upload date, size, and project.

The component does not expose a physical storage path. It refreshes the list
only after a committed service result and displays only authorized `Ready`
rows.

## Authorized content endpoint

Preview/download uses an authenticated server-side stream boundary:

1. resolve the current cookie identity;
2. load document metadata;
3. invoke resource authorization;
4. open the opaque storage reference;
5. set safe content type/disposition and stream bytes;
6. record preview/download/access activity.

There is no static-file route to the storage root and no arbitrary physical
path endpoint.

## Later UI contracts

Search/filter/sort, project documents, Shared with Me, task attachments,
dashboard recent-five/count, notifications, and administrator audit reporting
reuse the service and authorization contracts. They are not required for the
US1 independent MVP test.
