using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Domain.IServices
{
    public interface ITasksService
    {
        Task<List<TaskViewModel>> GetProjectTasksAsync(Guid projectId, string? status = null, string? priority = null);
        Task<TaskViewModel> GetTaskByIdAsync(Guid taskId);
        Task<TaskViewModel> CreateTaskAsync(Guid userId, CreateTaskRequestViewModel model);
        Task<TaskViewModel> UpdateTaskAsync(Guid userId, UpdateTaskRequestViewModel model);
        Task<TaskViewModel> UpdateTaskStatusAsync(Guid userId, UpdateTaskStatusRequestViewModel model);
        Task<bool> DeleteTaskAsync(Guid userId, Guid taskId);

        // Sprint 05: Subtasks
        Task<List<SubtaskViewModel>> GetSubtasksAsync(Guid parentTaskId);
        Task<SubtaskViewModel> CreateSubtaskAsync(Guid userId, CreateSubtaskRequestViewModel model);
        Task<bool> DeleteTaskWithSubtasksAsync(Guid userId, Guid parentTaskId);

        // Sprint 05: Multi-Assignees & Watchers
        Task<List<TaskAssigneeViewModel>> GetTaskAssigneesAsync(Guid taskId);
        Task<TaskAssigneeViewModel> AddTaskAssigneeAsync(Guid userId, AddTaskAssigneeRequestViewModel model);
        Task<bool> RemoveTaskAssigneeAsync(Guid userId, Guid taskId, Guid targetUserId);
        Task<List<TaskWatcherViewModel>> GetTaskWatchersAsync(Guid taskId);
        Task<bool> ToggleTaskWatcherAsync(Guid userId, Guid taskId);

        // Sprint 06: Checklists & Recurring Tasks
        Task<List<ChecklistItemViewModel>> GetChecklistItemsAsync(Guid taskId);
        Task<ChecklistItemViewModel> AddChecklistItemAsync(Guid userId, AddChecklistItemRequestViewModel model);
        Task<bool> ToggleChecklistItemAsync(Guid userId, Guid itemId);
        Task<bool> DeleteChecklistItemAsync(Guid userId, Guid itemId);
        Task<RecurringTaskRuleViewModel> SetRecurringTaskRuleAsync(Guid userId, SetRecurringTaskRuleRequestViewModel model);
        Task<RecurringTaskRuleViewModel?> GetRecurringTaskRuleAsync(Guid taskId);
        Task<int> ProcessDueRecurringTasksAsync();
    }
}
