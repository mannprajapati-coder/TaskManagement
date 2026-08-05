using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TaskPlatform.Shared.Constants;
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
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Auth
        public async Task<ApiResponse<AuthResponseViewModel>> RegisterAsync(RegisterRequestViewModel model)
        {
            return await PostAsync<RegisterRequestViewModel, AuthResponseViewModel>(ApiEndpoint.Auth.Register, model);
        }

        public async Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailRequestViewModel model)
        {
            return await PostAsync<VerifyEmailRequestViewModel, bool>(ApiEndpoint.Auth.VerifyEmail, model);
        }

        public async Task<ApiResponse<AuthResponseViewModel>> LoginAsync(LoginRequestViewModel model)
        {
            return await PostAsync<LoginRequestViewModel, AuthResponseViewModel>(ApiEndpoint.Auth.Login, model);
        }

        public async Task<ApiResponse<AuthResponseViewModel>> RefreshTokenAsync(RefreshTokenRequestViewModel model)
        {
            return await PostAsync<RefreshTokenRequestViewModel, AuthResponseViewModel>(ApiEndpoint.Auth.RefreshToken, model);
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequestViewModel model)
        {
            return await PostAsync<ForgotPasswordRequestViewModel, bool>(ApiEndpoint.Auth.ForgotPassword, model);
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequestViewModel model)
        {
            return await PostAsync<ResetPasswordRequestViewModel, bool>(ApiEndpoint.Auth.ResetPassword, model);
        }

        public async Task<ApiResponse<AuthResponseViewModel>> GoogleLoginAsync(GoogleLoginRequestViewModel model)
        {
            return await PostAsync<GoogleLoginRequestViewModel, AuthResponseViewModel>(ApiEndpoint.Auth.GoogleLogin, model);
        }

        public async Task<ApiResponse<MfaSetupResponseViewModel>> EnableMfaAsync(string accessToken)
        {
            return await PostWithTokenAsync<object, MfaSetupResponseViewModel>(ApiEndpoint.Auth.EnableMfa, new { }, accessToken);
        }

        public async Task<ApiResponse<AuthResponseViewModel>> VerifyMfaAsync(VerifyMfaRequestViewModel model)
        {
            return await PostAsync<VerifyMfaRequestViewModel, AuthResponseViewModel>(ApiEndpoint.Auth.VerifyMfa, model);
        }

        public async Task<ApiResponse<bool>> LogoutAsync(RefreshTokenRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<RefreshTokenRequestViewModel, bool>(ApiEndpoint.Auth.Logout, model, accessToken);
        }

        // User Management
        public async Task<ApiResponse<UserProfileViewModel>> GetMyProfileAsync(string accessToken)
        {
            return await GetWithTokenAsync<UserProfileViewModel>("api/v1/Users/GetMyProfile", accessToken);
        }

        public async Task<ApiResponse<UserProfileViewModel>> UpdateProfileAsync(UpdateProfileRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UpdateProfileRequestViewModel, UserProfileViewModel>("api/v1/Users/UpdateProfile", model, accessToken);
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<ChangePasswordRequestViewModel, bool>("api/v1/Users/ChangePassword", model, accessToken);
        }

        public async Task<ApiResponse<UserPreferenceViewModel>> GetMyPreferencesAsync(string accessToken)
        {
            return await GetWithTokenAsync<UserPreferenceViewModel>("api/v1/Users/GetMyPreferences", accessToken);
        }

        public async Task<ApiResponse<UserPreferenceViewModel>> UpdatePreferencesAsync(UserPreferenceViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UserPreferenceViewModel, UserPreferenceViewModel>("api/v1/Users/UpdatePreferences", model, accessToken);
        }

        public async Task<ApiResponse<List<ActiveSessionViewModel>>> GetMyActiveSessionsAsync(string accessToken)
        {
            return await GetWithTokenAsync<List<ActiveSessionViewModel>>("api/v1/Users/GetMyActiveSessions", accessToken);
        }

        public async Task<ApiResponse<bool>> RevokeSessionAsync(string sessionId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Users/RevokeSession/{sessionId}", accessToken);
        }

        // Workspace
        public async Task<ApiResponse<List<WorkspaceViewModel>>> GetMyWorkspacesAsync(string accessToken)
        {
            return await GetWithTokenAsync<List<WorkspaceViewModel>>("api/v1/Workspaces", accessToken);
        }

        public async Task<ApiResponse<WorkspaceViewModel>> GetWorkspaceByIdAsync(string workspaceId, string accessToken)
        {
            return await GetWithTokenAsync<WorkspaceViewModel>($"api/v1/Workspaces/{workspaceId}", accessToken);
        }

        public async Task<ApiResponse<WorkspaceViewModel>> CreateWorkspaceAsync(CreateWorkspaceRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<CreateWorkspaceRequestViewModel, WorkspaceViewModel>("api/v1/Workspaces/Create", model, accessToken);
        }

        public async Task<ApiResponse<WorkspaceViewModel>> UpdateWorkspaceSettingsAsync(UpdateWorkspaceSettingsRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UpdateWorkspaceSettingsRequestViewModel, WorkspaceViewModel>("api/v1/Workspaces/UpdateSettings", model, accessToken);
        }

        public async Task<ApiResponse<bool>> ArchiveWorkspaceAsync(string workspaceId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Workspaces/Archive/{workspaceId}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> UnarchiveWorkspaceAsync(string workspaceId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Workspaces/Unarchive/{workspaceId}", new { }, accessToken);
        }

        public async Task<ApiResponse<WorkspaceInviteViewModel>> InviteMembersAsync(InviteMembersRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<InviteMembersRequestViewModel, WorkspaceInviteViewModel>("api/v1/Workspaces/InviteMembers", model, accessToken);
        }

        public async Task<ApiResponse<bool>> JoinViaInviteAsync(string token, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Workspaces/JoinViaInvite/{token}", new { }, accessToken);
        }

        // Projects & Members
        public async Task<ApiResponse<List<ProjectViewModel>>> GetWorkspaceProjectsAsync(string workspaceId, string accessToken)
        {
            return await GetWithTokenAsync<List<ProjectViewModel>>($"api/v1/Projects/GetByWorkspace/{workspaceId}", accessToken);
        }

        public async Task<ApiResponse<ProjectViewModel>> GetProjectByIdAsync(string projectId, string accessToken)
        {
            return await GetWithTokenAsync<ProjectViewModel>($"api/v1/Projects/{projectId}", accessToken);
        }

        public async Task<ApiResponse<ProjectViewModel>> CreateProjectAsync(CreateProjectRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<CreateProjectRequestViewModel, ProjectViewModel>("api/v1/Projects/Create", model, accessToken);
        }

        public async Task<ApiResponse<ProjectViewModel>> UpdateProjectAsync(UpdateProjectRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UpdateProjectRequestViewModel, ProjectViewModel>("api/v1/Projects/Update", model, accessToken);
        }

        public async Task<ApiResponse<bool>> ArchiveProjectAsync(string projectId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Projects/Archive/{projectId}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> UnarchiveProjectAsync(string projectId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Projects/Unarchive/{projectId}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> ToggleFavoriteProjectAsync(string projectId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Projects/ToggleFavorite/{projectId}", new { }, accessToken);
        }

        public async Task<ApiResponse<List<ProjectMemberViewModel>>> GetProjectMembersAsync(string projectId, string accessToken)
        {
            return await GetWithTokenAsync<List<ProjectMemberViewModel>>($"api/v1/Projects/{projectId}/Members", accessToken);
        }

        public async Task<ApiResponse<ProjectMemberViewModel>> AddProjectMemberAsync(AddProjectMemberRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<AddProjectMemberRequestViewModel, ProjectMemberViewModel>("api/v1/Projects/AddMember", model, accessToken);
        }

        public async Task<ApiResponse<bool>> RemoveProjectMemberAsync(string projectId, string targetUserId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Projects/{projectId}/RemoveMember/{targetUserId}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> UpdateProjectMemberRoleAsync(string projectId, string targetUserId, string role, string accessToken)
        {
            return await PutWithTokenAsync<string, bool>($"api/v1/Projects/{projectId}/UpdateMemberRole/{targetUserId}", role, accessToken);
        }

        public async Task<ApiResponse<ProjectJoinRequestViewModel>> SubmitProjectJoinRequestAsync(string projectId, string accessToken)
        {
            return await PostWithTokenAsync<object, ProjectJoinRequestViewModel>($"api/v1/Projects/SubmitJoinRequest/{projectId}", new { }, accessToken);
        }

        public async Task<ApiResponse<List<ProjectJoinRequestViewModel>>> GetPendingProjectJoinRequestsAsync(string projectId, string accessToken)
        {
            return await GetWithTokenAsync<List<ProjectJoinRequestViewModel>>($"api/v1/Projects/{projectId}/PendingJoinRequests", accessToken);
        }

        public async Task<ApiResponse<bool>> ResolveProjectJoinRequestAsync(ResolveJoinRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<ResolveJoinRequestViewModel, bool>("api/v1/Projects/ResolveJoinRequest", model, accessToken);
        }

        // Core Task Engine
        public async Task<ApiResponse<List<TaskViewModel>>> GetProjectTasksAsync(string projectId, string? status, string? priority, string accessToken)
        {
            var url = $"api/v1/Tasks/GetByProject/{projectId}";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (!string.IsNullOrEmpty(priority)) queryParams.Add($"priority={priority}");
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

            return await GetWithTokenAsync<List<TaskViewModel>>(url, accessToken);
        }

        public async Task<ApiResponse<TaskViewModel>> GetTaskByIdAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<TaskViewModel>($"api/v1/Tasks/{taskId}", accessToken);
        }

        public async Task<ApiResponse<TaskViewModel>> CreateTaskAsync(CreateTaskRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<CreateTaskRequestViewModel, TaskViewModel>("api/v1/Tasks/Create", model, accessToken);
        }

        public async Task<ApiResponse<TaskViewModel>> UpdateTaskAsync(UpdateTaskRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UpdateTaskRequestViewModel, TaskViewModel>("api/v1/Tasks/Update", model, accessToken);
        }

        public async Task<ApiResponse<TaskViewModel>> UpdateTaskStatusAsync(UpdateTaskStatusRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<UpdateTaskStatusRequestViewModel, TaskViewModel>("api/v1/Tasks/UpdateStatus", model, accessToken);
        }

        public async Task<ApiResponse<TaskViewModel>> ReorderTasksAsync(ReorderTasksRequestViewModel model, string accessToken)
        {
            return await PutWithTokenAsync<ReorderTasksRequestViewModel, TaskViewModel>("api/v1/Tasks/Reorder", model, accessToken);
        }

        public async Task<ApiResponse<bool>> DeleteTaskAsync(string taskId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Tasks/{taskId}", accessToken);
        }

        // Subtasks & Task Assignment
        public async Task<ApiResponse<List<SubtaskViewModel>>> GetSubtasksAsync(string parentTaskId, string accessToken)
        {
            return await GetWithTokenAsync<List<SubtaskViewModel>>($"api/v1/Tasks/{parentTaskId}/Subtasks", accessToken);
        }

        public async Task<ApiResponse<SubtaskViewModel>> CreateSubtaskAsync(CreateSubtaskRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<CreateSubtaskRequestViewModel, SubtaskViewModel>("api/v1/Tasks/CreateSubtask", model, accessToken);
        }

        public async Task<ApiResponse<bool>> DeleteTaskWithSubtasksAsync(string parentTaskId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Tasks/{parentTaskId}/DeleteWithSubtasks", accessToken);
        }

        public async Task<ApiResponse<List<TaskAssigneeViewModel>>> GetTaskAssigneesAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<List<TaskAssigneeViewModel>>($"api/v1/Tasks/{taskId}/Assignees", accessToken);
        }

        public async Task<ApiResponse<TaskAssigneeViewModel>> AddTaskAssigneeAsync(AddTaskAssigneeRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<AddTaskAssigneeRequestViewModel, TaskAssigneeViewModel>("api/v1/Tasks/AddAssignee", model, accessToken);
        }

        public async Task<ApiResponse<bool>> RemoveTaskAssigneeAsync(string taskId, string targetUserId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Tasks/{taskId}/RemoveAssignee/{targetUserId}", new { }, accessToken);
        }

        public async Task<ApiResponse<List<TaskWatcherViewModel>>> GetTaskWatchersAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<List<TaskWatcherViewModel>>($"api/v1/Tasks/{taskId}/Watchers", accessToken);
        }

        public async Task<ApiResponse<bool>> ToggleTaskWatcherAsync(string taskId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Tasks/{taskId}/ToggleWatcher", new { }, accessToken);
        }

        // Checklists
        public async Task<ApiResponse<List<ChecklistItemViewModel>>> GetChecklistItemsAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<List<ChecklistItemViewModel>>($"api/v1/Tasks/{taskId}/Checklists", accessToken);
        }

        public async Task<ApiResponse<ChecklistItemViewModel>> AddChecklistItemAsync(AddChecklistItemRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<AddChecklistItemRequestViewModel, ChecklistItemViewModel>("api/v1/Tasks/AddChecklistItem", model, accessToken);
        }

        public async Task<ApiResponse<bool>> ToggleChecklistItemAsync(string itemId, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Tasks/ToggleChecklistItem/{itemId}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> DeleteChecklistItemAsync(string itemId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Tasks/DeleteChecklistItem/{itemId}", accessToken);
        }

        // Recurring Task Rule
        public async Task<ApiResponse<RecurringTaskRuleViewModel>> GetRecurringTaskRuleAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<RecurringTaskRuleViewModel>($"api/v1/Tasks/{taskId}/RecurringRule", accessToken);
        }

        public async Task<ApiResponse<RecurringTaskRuleViewModel>> SetRecurringTaskRuleAsync(SetRecurringTaskRuleRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<SetRecurringTaskRuleRequestViewModel, RecurringTaskRuleViewModel>("api/v1/Tasks/SetRecurringRule", model, accessToken);
        }

        // Comments
        public async Task<ApiResponse<List<CommentViewModel>>> GetTaskCommentsAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<List<CommentViewModel>>($"api/v1/Collaboration/Tasks/{taskId}/Comments", accessToken);
        }

        public async Task<ApiResponse<CommentViewModel>> AddTaskCommentAsync(AddCommentRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<AddCommentRequestViewModel, CommentViewModel>("api/v1/Collaboration/Comments/Add", model, accessToken);
        }

        public async Task<ApiResponse<bool>> DeleteTaskCommentAsync(string commentId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Collaboration/Comments/{commentId}", accessToken);
        }

        // Attachments
        public async Task<ApiResponse<List<AttachmentViewModel>>> GetTaskAttachmentsAsync(string taskId, string accessToken)
        {
            return await GetWithTokenAsync<List<AttachmentViewModel>>($"api/v1/Collaboration/Tasks/{taskId}/Attachments", accessToken);
        }

        public async Task<ApiResponse<AttachmentViewModel>> AddTaskAttachmentAsync(string taskId, string fileName, string filePath, long fileSize, string contentType, string accessToken)
        {
            var query = $"api/v1/Collaboration/Attachments/Add?taskId={Uri.EscapeDataString(taskId)}" +
                        $"&fileName={Uri.EscapeDataString(fileName)}" +
                        $"&filePath={Uri.EscapeDataString(filePath)}" +
                        $"&fileSize={fileSize}" +
                        $"&contentType={Uri.EscapeDataString(contentType)}";
            return await PostWithTokenAsync<object, AttachmentViewModel>(query, new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> DeleteTaskAttachmentAsync(string attachmentId, string accessToken)
        {
            return await DeleteWithTokenAsync<bool>($"api/v1/Collaboration/Attachments/{attachmentId}", accessToken);
        }

        // Calendar & Dashboard
        public async Task<ApiResponse<List<CalendarEventViewModel>>> GetCalendarEventsAsync(string workspaceId, DateTime? start, DateTime? end, string accessToken)
        {
            var url = $"api/v1/Calendar/Events/{workspaceId}";
            var queryParams = new List<string>();
            if (start.HasValue) queryParams.Add($"start={start.Value:yyyy-MM-dd}");
            if (end.HasValue) queryParams.Add($"end={end.Value:yyyy-MM-dd}");
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

            return await GetWithTokenAsync<List<CalendarEventViewModel>>(url, accessToken);
        }

        public async Task<ApiResponse<bool>> RescheduleTaskAsync(RescheduleTaskDateRequestViewModel model, string accessToken)
        {
            return await PostWithTokenAsync<RescheduleTaskDateRequestViewModel, bool>("api/v1/Calendar/Reschedule", model, accessToken);
        }

        public async Task<ApiResponse<DashboardOverviewViewModel>> GetDashboardOverviewAsync(string workspaceId, string accessToken)
        {
            return await GetWithTokenAsync<DashboardOverviewViewModel>($"api/v1/Dashboard/Overview/{workspaceId}", accessToken);
        }

        // Notifications
        public async Task<ApiResponse<List<NotificationItemViewModel>>> GetMyNotificationsAsync(bool unreadOnly, string accessToken)
        {
            var url = "api/v1/Notifications";
            if (unreadOnly) url += "?unreadOnly=true";
            return await GetWithTokenAsync<List<NotificationItemViewModel>>(url, accessToken);
        }

        public async Task<ApiResponse<bool>> MarkNotificationAsReadAsync(string id, string accessToken)
        {
            return await PostWithTokenAsync<object, bool>($"api/v1/Notifications/MarkAsRead/{id}", new { }, accessToken);
        }

        public async Task<ApiResponse<bool>> MarkAllNotificationsAsReadAsync(string accessToken)
        {
            return await PostWithTokenAsync<object, bool>("api/v1/Notifications/MarkAllAsRead", new { }, accessToken);
        }

        // Helper Methods
        private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(path, model);
                return await ParseResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail($"API Error: {ex.Message}");
            }
        }

        private async Task<ApiResponse<TResponse>> GetWithTokenAsync<TResponse>(string path, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, path);
                SetToken(request, accessToken);
                var response = await _httpClient.SendAsync(request);
                return await ParseResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail($"API Error: {ex.Message}");
            }
        }

        private async Task<ApiResponse<TResponse>> PostWithTokenAsync<TRequest, TResponse>(string path, TRequest model, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = JsonContent.Create(model)
                };
                SetToken(request, accessToken);
                var response = await _httpClient.SendAsync(request);
                return await ParseResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail($"API Error: {ex.Message}");
            }
        }

        private async Task<ApiResponse<TResponse>> PutWithTokenAsync<TRequest, TResponse>(string path, TRequest model, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, path)
                {
                    Content = JsonContent.Create(model)
                };
                SetToken(request, accessToken);
                var response = await _httpClient.SendAsync(request);
                return await ParseResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail($"API Error: {ex.Message}");
            }
        }

        private async Task<ApiResponse<TResponse>> DeleteWithTokenAsync<TResponse>(string path, string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, path);
                SetToken(request, accessToken);
                var response = await _httpClient.SendAsync(request);
                return await ParseResponseAsync<TResponse>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail($"API Error: {ex.Message}");
            }
        }

        private static void SetToken(HttpRequestMessage request, string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        private static async Task<ApiResponse<TResponse>> ParseResponseAsync<TResponse>(HttpResponseMessage response)
        {
            var contentStr = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(contentStr))
            {
                return response.IsSuccessStatusCode
                    ? ApiResponse<TResponse>.Ok(default!)
                    : ApiResponse<TResponse>.Fail($"HTTP Request Failed with status code {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<TResponse>>(contentStr, options);
                if (result != null)
                {
                    return result;
                }
            }
            catch
            {
                // Response content is not ApiResponse<TResponse> JSON
            }

            if (response.IsSuccessStatusCode)
            {
                return ApiResponse<TResponse>.Fail("Invalid response format received from server.");
            }

            return ApiResponse<TResponse>.Fail($"HTTP Error {(int)response.StatusCode}: {contentStr}");
        }
    }
}
