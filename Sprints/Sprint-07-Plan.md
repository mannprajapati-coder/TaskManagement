# Sprint 07 — Comments & Attachments (Modules 16 & 17)

**Status:** Not Started
**Started:** —          **Completed:** —
**Actual days spent:** —   (Est. 5 days)

## Objective

Build task `Comments` (Module 16) with user `@mentions` and `Attachments` (Module 17) file upload and storage service.

## Included features / Requirements covered

- **Module 16**: Task Comments CRUD (`Tasks/{id}/Comments`), User `@mentions`.
- **Module 17**: File Attachment Upload (`Tasks/{id}/Attachments`), File Storage Abstraction (`IFileStorageService`).

## Task breakdown

1. **Comment Entity & Mentions** — Create `Comment` entity with markdown body and user mention parser.
2. **Attachment Entity & Storage Service** — Create `Attachment` entity and `IFileStorageService` implementation (Local disk storage for dev). Validate file extensions and size limits.
3. **API & Web UI** — Rich comment discussion thread on task detail view, file drag-and-drop upload zone, attachment preview/download.
4. **Tests** — Unit tests for mention parsing and file extension validation.

## Dependencies

- Sprint 04 — Task Engine

## Deliverables

- Migration `AddCommentsAndAttachmentsSchema`.
- Full comment thread UI and file upload attachment functionality.

## Acceptance criteria

- [ ] Users can post comments on tasks with `@user` mentions.
- [ ] Users can upload attachments with file extension validation and download uploaded files securely.
