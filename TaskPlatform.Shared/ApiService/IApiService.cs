using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Auth;
using TaskPlatform.Shared.ViewModels.Calendar;
using TaskPlatform.Shared.ViewModels.Collaboration;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Dashboard;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.Project;
using TaskPlatform.Shared.ViewModels.Task;
using TaskPlatform.Shared.ViewModels.User;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace TaskPlatform.Shared.ApiService
{
    public interface IApiService
    {
        // Auth
        Task<ApiResponse<AuthResponseViewModel>> RegisterAsync(RegisterRequestViewModel model);
        Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailRequestViewModel model);
        Task<ApiResponse<AuthResponseViewModel>> LoginAsync(LoginRequestViewModel model);
        Task<ApiResponse<AuthResponseViewModel>> RefreshTokenAsync(RefreshTokenRequestViewModel model);
        Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequestViewModel model);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequestViewModel model);
        Task<ApiResponse<AuthResponseViewModel>> GoogleLoginAsync(GoogleLoginRequestViewModel model);
        Task<ApiResponse<MfaSetupResponseViewModel>> EnableMfaAsync(string accessToken);
        Task<ApiResponse<AuthResponseViewModel>> VerifyMfaAsync(VerifyMfaRequestViewModel model);
        Task<ApiResponse<bool>> LogoutAsync(RefreshTokenRequestViewModel model, string accessToken);

        // User Management
        Task<ApiResponse<UserProfileViewModel>> GetMyProfileAsync(string accessToken);
        Task<ApiResponse<UserProfileViewModel>> UpdateProfileAsync(UpdateProfileRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequestViewModel model, string accessToken);
        Task<ApiResponse<UserPreferenceViewModel>> GetMyPreferencesAsync(string accessToken);
        Task<ApiResponse<UserPreferenceViewModel>> UpdatePreferencesAsync(UserPreferenceViewModel model, string accessToken);
        Task<ApiResponse<List<ActiveSessionViewModel>>> GetMyActiveSessionsAsync(string accessToken);
        Task<ApiResponse<bool>> RevokeSessionAsync(string sessionId, string accessToken);

        // Workspace
        Task<ApiResponse<List<WorkspaceViewModel>>> GetMyWorkspacesAsync(string accessToken);
        Task<ApiResponse<WorkspaceViewModel>> GetWorkspaceByIdAsync(string workspaceId, string accessToken);
        Task<ApiResponse<WorkspaceViewModel>> CreateWorkspaceAsync(CreateWorkspaceRequestViewModel model, string accessToken);
        Task<ApiResponse<WorkspaceViewModel>> UpdateWorkspaceSettingsAsync(UpdateWorkspaceSettingsRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> ArchiveWorkspaceAsync(string workspaceId, string accessToken);
        Task<ApiResponse<bool>> UnarchiveWorkspaceAsync(string workspaceId, string accessToken);
        Task<ApiResponse<WorkspaceInviteViewModel>> InviteMembersAsync(InviteMembersRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> JoinViaInviteAsync(string token, string accessToken);

        // Projects & Members
        Task<ApiResponse<List<ProjectViewModel>>> GetWorkspaceProjectsAsync(string workspaceId, string accessToken);
        Task<ApiResponse<ProjectViewModel>> GetProjectByIdAsync(string projectId, string accessToken);
        Task<ApiResponse<ProjectViewModel>> CreateProjectAsync(CreateProjectRequestViewModel model, string accessToken);
        Task<ApiResponse<ProjectViewModel>> UpdateProjectAsync(UpdateProjectRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> ArchiveProjectAsync(string projectId, string accessToken);
        Task<ApiResponse<bool>> UnarchiveProjectAsync(string projectId, string accessToken);
        Task<ApiResponse<bool>> ToggleFavoriteProjectAsync(string projectId, string accessToken);
        Task<ApiResponse<List<ProjectMemberViewModel>>> GetProjectMembersAsync(string projectId, string accessToken);
        Task<ApiResponse<ProjectMemberViewModel>> AddProjectMemberAsync(AddProjectMemberRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> RemoveProjectMemberAsync(string projectId, string targetUserId, string accessToken);
        Task<ApiResponse<bool>> UpdateProjectMemberRoleAsync(string projectId, string targetUserId, string role, string accessToken);
        Task<ApiResponse<ProjectJoinRequestViewModel>> SubmitProjectJoinRequestAsync(string projectId, string accessToken);
        Task<ApiResponse<List<ProjectJoinRequestViewModel>>> GetPendingProjectJoinRequestsAsync(string projectId, string accessToken);
        Task<ApiResponse<bool>> ResolveProjectJoinRequestAsync(ResolveJoinRequestViewModel model, string accessToken);

        // Core Task Engine
        Task<ApiResponse<List<TaskViewModel>>> GetProjectTasksAsync(string projectId, string? status, string? priority, string accessToken);
        Task<ApiResponse<TaskViewModel>> GetTaskByIdAsync(string taskId, string accessToken);
        Task<ApiResponse<TaskViewModel>> CreateTaskAsync(CreateTaskRequestViewModel model, string accessToken);
        Task<ApiResponse<TaskViewModel>> UpdateTaskAsync(UpdateTaskRequestViewModel model, string accessToken);
        Task<ApiResponse<TaskViewModel>> UpdateTaskStatusAsync(UpdateTaskStatusRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> DeleteTaskAsync(string taskId, string accessToken);

        // Subtasks & Task Assignment
        Task<ApiResponse<List<SubtaskViewModel>>> GetSubtasksAsync(string parentTaskId, string accessToken);
        Task<ApiResponse<SubtaskViewModel>> CreateSubtaskAsync(CreateSubtaskRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> DeleteTaskWithSubtasksAsync(string parentTaskId, string accessToken);
        Task<ApiResponse<List<TaskAssigneeViewModel>>> GetTaskAssigneesAsync(string taskId, string accessToken);
        Task<ApiResponse<TaskAssigneeViewModel>> AddTaskAssigneeAsync(AddTaskAssigneeRequestViewModel model, string accessToken);
        Task<ApiResponse<bool>> RemoveTaskAssigneeAsync(string taskId, string targetUserId, string accessToken);
        Task<ApiResponse<List<TaskWatcherViewModel>>> GetTaskWatchersAsync(string taskId, string accessToken);
        Task<ApiResponse<bool>> ToggleTaskWatcherAsync(string taskId, string accessToken);

        // Calendar & Dashboard
        Task<ApiResponse<List<CalendarEventViewModel>>> GetCalendarEventsAsync(string workspaceId, string accessToken);
        Task<ApiResponse<bool>> RescheduleTaskAsync(RescheduleTaskDateRequestViewModel model, string accessToken);
        Task<ApiResponse<DashboardOverviewViewModel>> GetDashboardOverviewAsync(string workspaceId, string accessToken);
    }
}
