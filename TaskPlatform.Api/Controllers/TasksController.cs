using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Modules.Notifications.Domain.IServices;
using Modules.Projects.Domain.IServices;
using Modules.Tasks.Domain.IServices;
using TaskPlatform.Api.Hubs;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.Task;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITasksService _tasksService;
        private readonly INotificationService _notificationService;
        private readonly IProjectsService _projectsService;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public TasksController(ITasksService tasksService, INotificationService notificationService, IProjectsService projectsService, IHubContext<NotificationsHub> hubContext)
        {
            _tasksService = tasksService;
            _notificationService = notificationService;
            _projectsService = projectsService;
            _hubContext = hubContext;
        }

        [HttpGet("GetByProject/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<TaskViewModel>>>> GetByProject(Guid projectId, [FromQuery] string? status, [FromQuery] string? priority)
        {
            var result = await _tasksService.GetProjectTasksAsync(projectId, status, priority);
            return Ok(ApiResponse<List<TaskViewModel>>.Ok(result));
        }

        [HttpGet("GetMyTasks")]
        public async Task<ActionResult<ApiResponse<List<TaskViewModel>>>> GetMyTasks([FromQuery] Guid? projectId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.GetMyTasksAsync(userId, projectId);
            return Ok(ApiResponse<List<TaskViewModel>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> GetById(Guid id)
        {
            var result = await _tasksService.GetTaskByIdAsync(id);
            return Ok(ApiResponse<TaskViewModel>.Ok(result));
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Create([FromBody] CreateTaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.CreateTaskAsync(userId, model);
            await LogActivityAsync(userId, result.ProjectId, result.Id, "TaskCreated", $"Created task \"{result.Title}\".");
            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Task created successfully."));
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Update([FromBody] UpdateTaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var before = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var result = await _tasksService.UpdateTaskAsync(userId, model);

            if (before.DueDate != result.DueDate)
            {
                await NotifyDueDateChangeAsync(result, userId);
            }

            await LogActivityAsync(userId, result.ProjectId, result.Id, "TaskUpdated", $"Updated task \"{result.Title}\".");
            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Task updated successfully."));
        }

        [HttpPut("UpdateStatus")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> UpdateStatus([FromBody] UpdateTaskStatusRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var before = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var result = await _tasksService.UpdateTaskStatusAsync(userId, model);

            if (before.Status != result.Status)
            {
                await NotifyStatusChangeAsync(result, userId);
                await LogActivityAsync(userId, result.ProjectId, result.Id, "StatusChanged", $"Status changed from {before.Status} to {result.Status}.");
            }

            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Task status updated."));
        }

        [HttpPut("Reorder")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Reorder([FromBody] ReorderTasksRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var before = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var result = await _tasksService.ReorderTasksAsync(userId, model);

            if (before.Status != result.Status)
            {
                await NotifyStatusChangeAsync(result, userId);
                await LogActivityAsync(userId, result.ProjectId, result.Id, "StatusChanged", $"Status changed from {before.Status} to {result.Status}.");
            }

            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Tasks reordered."));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var task = await _tasksService.GetTaskByIdAsync(id);
            var result = await _tasksService.DeleteTaskAsync(userId, id);
            await LogActivityAsync(userId, task.ProjectId, null, "TaskDeleted", $"Deleted task \"{task.Title}\".");
            return Ok(ApiResponse<bool>.Ok(result, "Task deleted."));
        }

        // Subtasks Endpoints
        [HttpGet("{parentTaskId}/Subtasks")]
        public async Task<ActionResult<ApiResponse<List<SubtaskViewModel>>>> GetSubtasks(Guid parentTaskId)
        {
            var result = await _tasksService.GetSubtasksAsync(parentTaskId);
            return Ok(ApiResponse<List<SubtaskViewModel>>.Ok(result));
        }

        [HttpPost("CreateSubtask")]
        public async Task<ActionResult<ApiResponse<SubtaskViewModel>>> CreateSubtask([FromBody] CreateSubtaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.CreateSubtaskAsync(userId, model);
            await LogActivityAsync(userId, result.ProjectId, result.ParentTaskId, "SubtaskCreated", $"Added subtask \"{result.Title}\".");
            return Ok(ApiResponse<SubtaskViewModel>.Ok(result, "Subtask created successfully."));
        }

        [HttpDelete("{parentTaskId}/DeleteWithSubtasks")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteWithSubtasks(Guid parentTaskId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.DeleteTaskWithSubtasksAsync(userId, parentTaskId);
            return Ok(ApiResponse<bool>.Ok(result, "Parent task and subtasks deleted."));
        }

        // Multi-Assignees & Watchers Endpoints
        [HttpGet("{taskId}/Assignees")]
        public async Task<ActionResult<ApiResponse<List<TaskAssigneeViewModel>>>> GetAssignees(Guid taskId)
        {
            var result = await _tasksService.GetTaskAssigneesAsync(taskId);
            return Ok(ApiResponse<List<TaskAssigneeViewModel>>.Ok(result));
        }

        [HttpPost("AddAssignee")]
        public async Task<ActionResult<ApiResponse<TaskAssigneeViewModel>>> AddAssignee([FromBody] AddTaskAssigneeRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.AddTaskAssigneeAsync(userId, model);

            var task = await _tasksService.GetTaskByIdAsync(model.TaskId);
            await NotifyUserAsync(model.UserId, userId,
                "You've been assigned a task",
                $"\"{task.Title}\" was assigned to you.",
                $"/Task/Detail/{task.Id}");
            await LogActivityAsync(userId, task.ProjectId, task.Id, "AssigneeAdded", $"Assigned {result.FullName} to \"{task.Title}\".");

            return Ok(ApiResponse<TaskAssigneeViewModel>.Ok(result, "Assignee added to task."));
        }

        [HttpPost("{taskId}/RemoveAssignee/{targetUserId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAssignee(Guid taskId, Guid targetUserId)
        {
            var userId = GetCurrentUserId();
            var task = await _tasksService.GetTaskByIdAsync(taskId);
            var result = await _tasksService.RemoveTaskAssigneeAsync(userId, taskId, targetUserId);
            await LogActivityAsync(userId, task.ProjectId, task.Id, "AssigneeRemoved", $"Removed an assignee from \"{task.Title}\".");
            return Ok(ApiResponse<bool>.Ok(result, "Assignee removed."));
        }

        [HttpGet("{taskId}/Watchers")]
        public async Task<ActionResult<ApiResponse<List<TaskWatcherViewModel>>>> GetWatchers(Guid taskId)
        {
            var result = await _tasksService.GetTaskWatchersAsync(taskId);
            return Ok(ApiResponse<List<TaskWatcherViewModel>>.Ok(result));
        }

        [HttpPost("{taskId}/ToggleWatcher")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleWatcher(Guid taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.ToggleTaskWatcherAsync(userId, taskId);
            return Ok(ApiResponse<bool>.Ok(result, "Watcher status toggled."));
        }

        // Checklist Endpoints
        [HttpGet("{taskId}/Checklists")]
        public async Task<ActionResult<ApiResponse<List<ChecklistItemViewModel>>>> GetChecklistItems(Guid taskId)
        {
            var result = await _tasksService.GetChecklistItemsAsync(taskId);
            return Ok(ApiResponse<List<ChecklistItemViewModel>>.Ok(result));
        }

        [HttpPost("AddChecklistItem")]
        public async Task<ActionResult<ApiResponse<ChecklistItemViewModel>>> AddChecklistItem([FromBody] AddChecklistItemRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.AddChecklistItemAsync(userId, model);
            var task = await _tasksService.GetTaskByIdAsync(result.TaskId);
            await LogActivityAsync(userId, task.ProjectId, task.Id, "ChecklistItemAdded", $"Added checklist item \"{result.Title}\".");
            return Ok(ApiResponse<ChecklistItemViewModel>.Ok(result, "Checklist item added."));
        }

        [HttpPost("ToggleChecklistItem/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleChecklistItem(Guid itemId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.ToggleChecklistItemAsync(userId, itemId);
            return Ok(ApiResponse<bool>.Ok(result, "Checklist item toggled."));
        }

        [HttpDelete("DeleteChecklistItem/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteChecklistItem(Guid itemId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.DeleteChecklistItemAsync(userId, itemId);
            return Ok(ApiResponse<bool>.Ok(result, "Checklist item deleted."));
        }

        // Recurring Task Endpoints
        [HttpPost("SetRecurringRule")]
        public async Task<ActionResult<ApiResponse<RecurringTaskRuleViewModel>>> SetRecurringRule([FromBody] SetRecurringTaskRuleRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.SetRecurringTaskRuleAsync(userId, model);
            var task = await _tasksService.GetTaskByIdAsync(result.TaskId);
            await LogActivityAsync(userId, task.ProjectId, task.Id, "RecurringRuleSet", $"Set recurring rule ({result.RecurrencePattern}) on \"{task.Title}\".");
            return Ok(ApiResponse<RecurringTaskRuleViewModel>.Ok(result, "Recurring task rule saved."));
        }

        [HttpGet("{taskId}/RecurringRule")]
        public async Task<ActionResult<ApiResponse<RecurringTaskRuleViewModel>>> GetRecurringRule(Guid taskId)
        {
            var result = await _tasksService.GetRecurringTaskRuleAsync(taskId);
            return Ok(ApiResponse<RecurringTaskRuleViewModel>.Ok(result));
        }

        [HttpPost("ProcessRecurringTasks")]
        public async Task<ActionResult<ApiResponse<int>>> ProcessRecurringTasks()
        {
            var count = await _tasksService.ProcessDueRecurringTasksAsync();
            return Ok(ApiResponse<int>.Ok(count, $"Processed recurring tasks. Created {count} new tasks."));
        }

        private async Task NotifyStatusChangeAsync(TaskViewModel task, Guid currentUserId)
        {
            var recipients = await GetNotificationRecipientsAsync(task);
            foreach (var recipientId in recipients)
            {
                await NotifyUserAsync(recipientId, currentUserId,
                    "Task status updated",
                    $"\"{task.Title}\" status changed to {task.Status}.",
                    $"/Task/Detail/{task.Id}");
            }
        }

        private async Task NotifyDueDateChangeAsync(TaskViewModel task, Guid currentUserId)
        {
            var recipients = await GetNotificationRecipientsAsync(task);
            var dueDateText = task.DueDate.HasValue ? task.DueDate.Value.ToString("MMM dd, yyyy") : "no due date";

            foreach (var recipientId in recipients)
            {
                await NotifyUserAsync(recipientId, currentUserId,
                    "Task due date changed",
                    $"\"{task.Title}\" due date changed to {dueDateText}.",
                    $"/Task/Detail/{task.Id}");
            }
        }

        // BR-12-02: Watchers are notified of Status changes, new Comments, and due-date changes only.
        private async Task<HashSet<Guid>> GetNotificationRecipientsAsync(TaskViewModel task)
        {
            var recipients = new HashSet<Guid>();
            if (task.PrimaryAssigneeUserId.HasValue)
            {
                recipients.Add(task.PrimaryAssigneeUserId.Value);
            }

            var watchers = await _tasksService.GetTaskWatchersAsync(task.Id);
            foreach (var watcher in watchers)
            {
                recipients.Add(watcher.UserId);
            }

            return recipients;
        }

        private async Task LogActivityAsync(Guid actorUserId, Guid projectId, Guid? taskId, string action, string details)
        {
            try
            {
                var project = await _projectsService.GetProjectByIdAsync(projectId, actorUserId);
                await _notificationService.LogActivityAsync(actorUserId, new CreateActivityLogRequestViewModel
                {
                    WorkspaceId = project.WorkspaceId,
                    ProjectId = projectId,
                    TaskId = taskId,
                    Action = action,
                    Details = details
                });
            }
            catch
            {
                // Activity logging is best-effort and must never fail the primary task operation.
            }
        }

        private async Task NotifyUserAsync(Guid targetUserId, Guid currentUserId, string title, string message, string linkUrl)
        {
            if (targetUserId == currentUserId)
            {
                return;
            }

            var notification = await _notificationService.SendNotificationAsync(new SendNotificationRequestViewModel
            {
                UserId = targetUserId,
                Title = title,
                Message = message,
                LinkUrl = linkUrl
            });

            await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("ReceiveNotification", notification);
        }

        private Guid GetCurrentUserId()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId;
        }
    }
}
