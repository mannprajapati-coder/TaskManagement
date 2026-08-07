using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Domain.IServices
{
    public interface ITasksService
    {
        Task<List<TaskViewModel>> GetProjectTasksAsync(Guid projectId, string? status = null, string? priority = null);
        Task<List<TaskViewModel>> GetMyTasksAsync(Guid userId, Guid? projectId = null);
        Task<ApiResponse<TaskViewModel>> GetTaskByIdAsync(Guid taskId);
        Task<ApiResponse<TaskViewModel>> CreateTaskAsync(Guid userId, CreateTaskRequestViewModel model);
        Task<ApiResponse<TaskViewModel>> UpdateTaskAsync(Guid userId, UpdateTaskRequestViewModel model);
        Task<ApiResponse<TaskViewModel>> UpdateTaskStatusAsync(Guid userId, UpdateTaskStatusRequestViewModel model);
        Task<ApiResponse<TaskViewModel>> ReorderTasksAsync(Guid userId, ReorderTasksRequestViewModel model);
        Task<ApiResponse<bool>> DeleteTaskAsync(Guid userId, Guid taskId);

        // Sprint 05: Subtasks
        Task<List<SubtaskViewModel>> GetSubtasksAsync(Guid parentTaskId);
        Task<ApiResponse<SubtaskViewModel>> CreateSubtaskAsync(Guid userId, CreateSubtaskRequestViewModel model);
        Task<ApiResponse<bool>> DeleteTaskWithSubtasksAsync(Guid userId, Guid parentTaskId);

        // Sprint 05: Multi-Assignees & Watchers
        Task<List<TaskAssigneeViewModel>> GetTaskAssigneesAsync(Guid taskId);
        Task<ApiResponse<TaskAssigneeViewModel>> AddTaskAssigneeAsync(Guid userId, AddTaskAssigneeRequestViewModel model);
        Task<ApiResponse<bool>> RemoveTaskAssigneeAsync(Guid userId, Guid taskId, Guid targetUserId);
        Task<List<TaskWatcherViewModel>> GetTaskWatchersAsync(Guid taskId);
        Task<ApiResponse<bool>> ToggleTaskWatcherAsync(Guid userId, Guid taskId);

        // Sprint 06: Checklists & Recurring Tasks
        Task<List<ChecklistItemViewModel>> GetChecklistItemsAsync(Guid taskId);
        Task<ApiResponse<ChecklistItemViewModel>> AddChecklistItemAsync(Guid userId, AddChecklistItemRequestViewModel model);
        Task<ApiResponse<bool>> ToggleChecklistItemAsync(Guid userId, Guid itemId);
        Task<ApiResponse<bool>> DeleteChecklistItemAsync(Guid userId, Guid itemId);
        Task<ApiResponse<RecurringTaskRuleViewModel>> SetRecurringTaskRuleAsync(Guid userId, SetRecurringTaskRuleRequestViewModel model);
        Task<RecurringTaskRuleViewModel?> GetRecurringTaskRuleAsync(Guid taskId);
        Task<int> ProcessDueRecurringTasksAsync();
    }
}
