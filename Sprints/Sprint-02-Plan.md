# Sprint 02 — User Management & Workspace (Modules 2 & 4)

**Status:** Completed
**Started:** 2026-08-05          **Completed:** 2026-08-05
**Actual days spent:** 1   (Est. 5 days)

## Objective

Build `UserManagement` (Module 2) profile/session management features and `Workspace` (Module 4) creation, settings, and member invite/joining capabilities. This provides users with profile control and multi-workspace organization.

## Included features / Requirements covered

- **Module 2**: Profile updating (`Users/UpdateProfile`, `Users/UploadProfilePicture`), Change Password (`Users/ChangePassword`), User Preferences (`Users/GetMyPreferences`, `Users/UpdatePreferences`), and Session Management (`Users/GetMyActiveSessions`, `Users/RevokeSession`).
- **Module 4**: Workspace creation (`Workspaces/Create`), Settings (`Workspaces/UpdateSettings`), Archive/Unarchive (`Workspaces/Archive`, `Workspaces/Unarchive`), Invites (`Workspaces/InviteMembers`, `Workspaces/JoinViaInvite/{token}`).

## Task breakdown

1. **User Profile & Preference entities** — Expand `User` entity with profile fields (`Bio`, `JobTitle`) and create `UserPreference` entity (`TimeZone`, `Language`, `NotificationChannelPrefs`) in `Modules/UserManagement`.
2. **`UserManagementDbContext` Update & Migration** — Add `UserPreference` and `ActiveSession` tables to `UserManagementDbContext`. Generate migration `AddUserProfileSchema`.
3. **`UsersController` API & Service** — Build `IUserService` and `UsersController` for profile management, password change, preferences, active session listing and remote session revocation.
4. **Workspace entity & `WorkspacesDbContext`** — Create `Workspace` and `WorkspaceInvite` entities in `Modules/Workspaces`. Create `WorkspacesDbContext` with tenant global query filters.
5. **`WorkspacesController` API & Service** — Build `IWorkspaceService` and `WorkspacesController` for creating workspaces, updating settings, soft archiving, generating workspace invite tokens, and accepting invites (BR-04-01).
6. **Web MVC Controllers & Views** — 
   - `UsersController.cs` in `TaskPlatform.Web` + `Profile.cshtml`, `Preferences.cshtml`, `Sessions.cshtml`.
   - `WorkspaceController.cs` in `TaskPlatform.Web` + `Index.cshtml`, `Create.cshtml`, `Settings.cshtml`, `Join.cshtml`.
7. **Tests** — Unit tests for BR-04-01 (Invite token expiry & max uses) and BR-04-02 (Archiving workspace hides from default list).

## Dependencies

- Sprint 01 — Authentication

## Deliverables

- Working `UsersController` and `WorkspacesController` API endpoints.
- Web MVC Profile, Workspace management, and Join via invite pages.
- Migrations `AddUserProfileSchema` and `InitialWorkspacesSchema`.

## Acceptance criteria

- [x] Users can edit profile details, upload profile picture, and change password.
- [x] Users can view active sessions across devices and revoke specific sessions.
- [x] Users can create Workspaces, generate invite links, and join Workspaces via invite token.
- [x] Expired or maxed-out workspace invite tokens fail with specific validation error (BR-04-01).
- [x] EF Core migration applies cleanly to local DB.
