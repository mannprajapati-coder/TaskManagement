# Folder / Solution Structure

```
TaskPlatform/                              (repo root)
├── TaskPlatform.sln
├── docs/                                  ← you are here (this whole set of 37 files)
├── db/                                    ← EF Core migration bundles land here too, see migrations.md
│
├── Modules/
│   ├── Authentication/                    Module 1
│   │   ├── Authentication.csproj
│   │   ├── Domain/
│   │   │   ├── Entities/                  RefreshToken, EmailVerificationToken, PasswordResetToken, MfaSecret
│   │   │   └── IServices/                 IAuthService
│   │   ├── Application/
│   │   │   ├── Services/                  AuthService
│   │   │   ├── Profiles/                  AutoMapper profiles
│   │   │   └── Extensions/                ServiceCollectionExtensions (AddAuthenticationModule)
│   │   └── Infrastructure/
│   │       ├── Context/                   AuthenticationDbContext
│   │       └── Repositories/              RefreshTokenRepository, ...
│   ├── UserManagement/                    Module 2 (same internal shape)
│   ├── Organizations/                     Module 3
│   ├── Workspaces/                        Module 4
│   ├── Teams/                             Module 5
│   ├── RolesPermissions/                  Module 6
│   ├── Projects/                          Module 7
│   ├── ProjectMembers/                    Module 8
│   ├── Milestones/                        Module 9
│   ├── Tasks/                             Module 10 — the largest module, see note below
│   ├── Subtasks/                          Module 11
│   ├── TaskAssignment/                    Module 12
│   ├── Dependencies/                      Module 13
│   ├── Checklists/                        Module 14
│   ├── RecurringTasks/                    Module 15
│   ├── Comments/                          Module 16
│   ├── Attachments/                       Module 17
│   ├── ActivityTimeline/                  Module 18
│   ├── Notifications/                     Module 19
│   ├── Kanban/                            Module 20
│   ├── Calendar/                          Module 21
│   ├── GanttChart/                        Module 22
│   ├── TimeTracking/                      Module 23
│   ├── Dashboard/                         Module 24
│   ├── Reports/                           Module 25
│   ├── AIAssistant/                       Module 26
│   ├── AITaskGenerator/                   Module 27
│   ├── AISmartScheduler/                  Module 28
│   ├── AIMeetingNotes/                    Module 29
│   └── AIAnalytics/                       Module 30
│
├── TaskPlatform.Api/
│   ├── Controllers/                       one controller per module, e.g. TasksController, KanbanController
│   ├── Hubs/                              ActivityHub.cs (SignalR)
│   ├── Middleware/                        ExceptionHandlingMiddleware, TenantResolutionMiddleware
│   ├── BackgroundJobs/                    Hangfire job definitions (RecurringTaskGenerator, ReminderDispatcher, ...)
│   ├── Program.cs
│   └── appsettings*.json
│
├── TaskPlatform.Web/
│   ├── Controllers/                       one controller per feature area (thin — calls ApiService)
│   ├── Views/
│   │   ├── Shared/                        _Layout.cshtml, _Sidebar.cshtml (role-aware nav), _Header.cshtml
│   │   ├── Auth/  Dashboard/  Workspace/  Project/  Kanban/  Calendar/  Gantt/  Reports/  Ai/  ...
│   ├── wwwroot/
│   │   └── assets/                        css/js/images — see ui-guidelines.md
│   ├── Program.cs
│   └── appsettings*.json
│
├── TaskPlatform.Shared/                   mirrors Sawi.Helper
│   ├── ApiService/                        typed HttpClient wrapper Web uses to call Api
│   ├── ViewModels/                        request/response DTOs shared by Web and Api
│   ├── Enums/                             Role, TaskStatus, Priority, NotificationChannel, ...
│   ├── Constants/                         ApiEndPoint.cs (route constants), PermissionKeys.cs
│   ├── Attributes/                        RoleClaimAuthorizeFilter-equivalent
│   ├── Exceptions/                        DomainException, PermissionDeniedException
│   └── CommonMethod/                      WebAuthHelper-equivalent (login/cookie/claims helpers)
│
└── TaskPlatform.Tests/
    ├── Unit/                              one folder per module, mirrors Modules/
    ├── Integration/                       WebApplicationFactory-based API tests
    └── TaskPlatform.Tests.csproj
```

## Notes

- **Every module folder has the identical internal shape** (`Domain/Application/Infrastructure`) shown once above for `Authentication` — this is intentional and mechanical; a new module is always created by copying the shape, never inventing a new one. See [coding-standards.md](coding-standards.md) §"Adding a new module".
- **`Tasks/` is the largest module** because Task is the spec's central entity (status, priority, labels, due/start dates, estimated/actual hours). It stays a single module rather than being pre-split, per ADR-005 — the spec names it as one module (Module 10), and `Subtasks`/`TaskAssignment`/`Dependencies`/`Checklists`/`RecurringTasks` are already split out as their own modules (11–15) precisely so `Tasks` itself doesn't balloon.
- **Solution folders in `TaskPlatform.sln`** group the 30 module projects to match the spec's 8 phases (`Phase1-Foundation`, `Phase2-Workspace`, ... `Phase8-AI`) purely for IDE navigation — this has no effect on build output or namespaces.
- **No module ever has a project reference to another module's `.csproj` for its `Infrastructure` or `Domain.Entities`** — only to another module's `IServices` (via the referencing module's own `Application` layer registering the dependency through DI). This is enforced by convention + code review today; see [coding-standards.md](coding-standards.md) for the specific review checklist item.
