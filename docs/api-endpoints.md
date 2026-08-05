# API Endpoints

Endpoint list per module, following the conventions in [api-conventions.md](api-conventions.md). This is the planning-stage contract — treat it as the target for each phase in [plan.md](plan.md); update this file in the same PR that adds/changes a real controller action so it never drifts from the code (see [git-workflow.md](git-workflow.md)).

All paths are relative to `api/v1/`. `[Auth]` = requires `[Authorize]` + the permission noted; `[Anon]` = `[AllowAnonymous]`.

### Authentication (Module 1) — `AuthController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Auth/Register` | Anon | FR-01-1 |
| POST | `Auth/VerifyEmail` | Anon | |
| POST | `Auth/Login` | Anon | returns access + refresh token |
| POST | `Auth/RefreshToken` | Anon *(bearer refresh token)* | rotates per BR-01-03 |
| POST | `Auth/ForgotPassword` | Anon | |
| POST | `Auth/ResetPassword` | Anon | BR-01-01 |
| POST | `Auth/GoogleLogin` | Anon | FR-01-6 |
| POST | `Auth/EnableMfa` / `Auth/VerifyMfa` | Auth | FR-01-7 |
| POST | `Auth/Logout` | Auth | revokes current refresh token |

### User Management (Module 2) — `UsersController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Users/GetMyProfile` | Auth | |
| PUT | `Users/UpdateProfile` | Auth | |
| POST | `Users/UploadProfilePicture` | Auth | |
| PUT | `Users/ChangePassword` | Auth | requires current password |
| GET/PUT | `Users/GetMyPreferences` / `UpdatePreferences` | Auth | notification/timezone/language, BR-19-02 |
| GET | `Users/GetMyActiveSessions` | Auth | |
| DELETE | `Users/RevokeSession/{sessionId}` | Auth | |

### Organization (Module 3) — `OrganizationsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Organizations/Create` | Auth | creator becomes Owner, BR-03-01 |
| GET/PUT | `Organizations/GetSettings` / `UpdateSettings` | Auth: ManageBilling for billing fields, else Admin+ | |
| GET | `Organizations/GetSubscription` | Auth | |
| PUT | `Organizations/UpdateSubscription` | Auth: ManageBilling | |
| POST | `Organizations/TransferOwnership` | Auth: Owner | BR-03-01 |
| GET | `Organizations/GetAuditLog` | Auth: Admin+ | |

### Workspace (Module 4) — `WorkspacesController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Workspaces/Create` | Auth: CreateProject-tier or Admin+ | |
| POST | `Workspaces/InviteMembers` | Auth: ManageUsers | |
| POST | `Workspaces/JoinViaInvite/{token}` | Auth | BR-04-01 |
| PUT | `Workspaces/UpdateSettings` | Auth | |
| POST | `Workspaces/Archive` / `Unarchive` | Auth: Admin+ | BR-04-02 |
| POST | `Workspaces/TransferOwnership` | Auth | |

### Team Management (Module 5) — `TeamsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Teams/Create` | Auth | |
| POST | `Teams/AddMembers` / `RemoveMembers` | Auth: TeamLead-of-team or ManageUsers, BR-06-01 | |
| PUT | `Teams/UpdatePermissions` | Auth | |
| GET | `Teams/GetActivity` | Auth | |

### Role & Permission (Module 6) — `RolesController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Roles/GetAll` | Auth | returns the 8 system roles |
| GET | `Roles/GetPermissionMatrix` | Auth | the table in user-roles.md, as data |
| PUT | `Roles/UpdateProjectMemberRole` | Auth: Admin+/PM | project-scoped override |

### Project (Module 7) — `ProjectsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Projects/Create` | Auth: CreateProject | |
| GET | `Projects/GetById/{id}` / `GetAll` | Auth: member or ViewReports | |
| PUT | `Projects/Update` | Auth: PM-of-project or Admin+ | |
| POST | `Projects/Archive` | Auth: DeleteProject | |
| POST | `Projects/ToggleFavorite` | Auth | per-user, BR table `ProjectFavorite` |

### Project Members (Module 8) — part of `ProjectsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Projects/AddMember` / `RemoveMember` | Auth: ManageUsers (effective) | |
| PUT | `Projects/AssignMemberRole` | Auth | project-scoped role |
| POST | `Projects/RequestToJoin` | Auth | |
| POST | `Projects/ApproveJoinRequest` / `RejectJoinRequest` | Auth: ManageUsers (effective), BR-08-01 | |

### Milestones (Module 9) — `MilestonesController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Milestones/Create` | Auth | |
| GET | `Milestones/GetByProject/{projectId}` | Auth | includes computed Completion%, BR-09-01 |
| PUT | `Milestones/Update` | Auth | |
| POST | `Milestones/AddDependency` | Auth | |

### Task (Module 10) — `TasksController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Tasks/Create` | Auth | |
| GET | `Tasks/GetById/{id}` | Auth | |
| GET | `Tasks/GetByProject/{projectId}` | Auth | paged, filterable per api-conventions.md |
| GET | `Tasks/GetMyTasks` | Auth | drives "my tasks" dashboard widget |
| PUT | `Tasks/Update` | Auth | |
| DELETE | `Tasks/Delete/{id}` | Auth | BR-10-02 cascade confirmation |
| PUT | `Tasks/UpdateStatus` | Auth | re-checks BR-13-01, BR-11-01 |

### Subtasks (Module 11) — `SubtasksController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Subtasks/Create` | Auth | `ParentTaskId` required |
| PUT | `Subtasks/UpdateStatus` | Auth | independent of parent, BR-11-01 |

### Task Assignment (Module 12) — `TaskAssignmentController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `TaskAssignment/AssignUser` / `AssignTeam` | Auth: AssignTask | BR-12-01 |
| POST | `TaskAssignment/AddWatcher` / `RemoveWatcher` | Auth | |

### Dependencies (Module 13) — `DependenciesController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Dependencies/Create` | Auth | BR-13-02 cycle check |
| DELETE | `Dependencies/Remove/{id}` | Auth | |
| GET | `Dependencies/GetGraphForProject/{projectId}` | Auth | feeds Gantt |

### Checklist (Module 14) — `ChecklistController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Checklist/AddItem` | Auth | |
| PUT | `Checklist/ToggleComplete/{itemId}` | Auth | |
| PUT | `Checklist/Reorder` | Auth | |

### Recurring Tasks (Module 15) — `RecurringTasksController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `RecurringTasks/CreateRule` | Auth | |
| PUT | `RecurringTasks/UpdateSeries/{ruleId}` | Auth | BR-15-02 |
| POST | `RecurringTasks/CancelSeries/{ruleId}` | Auth | |

### Comments (Module 16) — `CommentsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Comments/Create` | Auth | @mentions parsed server-side, BR-16-02 |
| PUT | `Comments/Update/{id}` | Auth: author or ManageUsers-effective, BR-16-01 | |
| DELETE | `Comments/Delete/{id}` | Auth: same as above | soft delete |
| POST | `Comments/React` | Auth | |

### Attachments (Module 17) — `AttachmentsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Attachments/Upload` | Auth | BR-17-01/02, multipart |
| GET | `Attachments/GetVersions/{logicalId}` | Auth | |
| GET | `Attachments/Download/{id}` | Auth | |

### Activity Timeline (Module 18) — `ActivityController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Activity/GetForEntity/{entityType}/{entityId}` | Auth | |
| GET | `Activity/GetForProject/{projectId}` | Auth | filterable by user/action type |

### Notifications (Module 19) — `NotificationsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Notifications/GetMy` | Auth | |
| PUT | `Notifications/MarkRead/{id}` | Auth | |
| PUT | `Notifications/MarkAllRead` | Auth | |

### Kanban Board (Module 20) — `KanbanController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Kanban/GetBoard/{projectId}` | Auth | |
| PUT | `Kanban/MoveCard` | Auth | BR-20-02 re-validation |
| POST | `Kanban/AddColumn` / `PUT UpdateColumn` | Auth: PM/Admin+ | WIP limit, BR-20-01 |

### Calendar (Module 21) — `CalendarController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Calendar/GetView` | Auth | `?view=day\|week\|month&date=...` |
| PUT | `Calendar/RescheduleViaDragDrop` | Auth | updates Task's dates |

### Gantt Chart (Module 22) — `GanttChartController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `GanttChart/GetForProject/{projectId}` | Auth | read-only in v1, see scope.md |

### Time Tracking (Module 23) — `TimeTrackingController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `TimeTracking/StartTimer/{taskId}` | Auth | auto-stops prior timer, BR-23-01 |
| POST | `TimeTracking/PauseTimer` / `ResumeTimer` / `StopTimer` | Auth | |
| GET | `TimeTracking/GetTimesheet` | Auth | `?userId=&from=&to=` |

### Dashboard (Module 24) — `DashboardController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `Dashboard/GetWidgets` | Auth | pending/overdue/workload/completion/productivity |

### Reports (Module 25) — `ReportsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `Reports/Generate` | Auth: ViewReports | |
| GET | `Reports/GetHistory` | Auth | |
| GET | `Reports/Export/{reportId}?format=pdf\|excel\|csv` | Auth: ExportReports, BR-25-01 | |

### AI Assistant (Module 26) — `AiAssistantController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `AiAssistant/Ask` | Auth | scoped to caller's own visible data |
| GET | `AiAssistant/GetConversation/{id}` | Auth | |

### AI Task Generator (Module 27) — `AiTaskGeneratorController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `AiTaskGenerator/GeneratePlan` | Auth | returns draft, BR-27-01 |
| POST | `AiTaskGenerator/AcceptPlan/{planId}` | Auth: CreateProject | commits via TasksService |

### AI Smart Scheduler (Module 28) — `AiSmartSchedulerController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `AiSmartScheduler/GetSuggestions` | Auth | BR-28-01 |
| POST | `AiSmartScheduler/AcceptSuggestion/{id}` | Auth | |

### AI Meeting Notes (Module 29) — `AiMeetingNotesController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `AiMeetingNotes/UploadTranscript` | Auth | BR-29-01 |
| POST | `AiMeetingNotes/AcceptExtractedItem/{id}` | Auth | commits via TasksService |

### AI Analytics (Module 30) — `AiAnalyticsController`
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `AiAnalytics/GetProjectRisk/{projectId}` | Auth: ViewReports | |
| GET | `AiAnalytics/GetTeamPerformance/{teamId}` | Auth: ViewReports | |

---

Every endpoint above is `[Authorize]` unless marked `Anon`; the specific permission noted is enforced server-side per [api-conventions.md](api-conventions.md) — this table states intent, the actual filter attribute on the controller action is the enforced source of truth once built.
