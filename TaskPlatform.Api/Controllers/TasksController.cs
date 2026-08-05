using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Modules.Notifications.Domain.IServices;
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
        private readonly IHubContext<NotificationsHub> _hubContext;

        public TasksController(ITasksService tasksService, INotificationService notificationService, IHubContext<NotificationsHub> hubContext)
        {
            _tasksService = tasksService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        [HttpGet("GetByProject/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<TaskViewModel>>>> GetByProject(Guid projectId, [FromQuery] string? status, [FromQuery] string? priority)
        {
            var result = await _tasksService.GetProjectTasksAsync(projectId, status, priority);
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
            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Task created successfully."));
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Update([FromBody] UpdateTaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.UpdateTaskAsync(userId, model);
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
            }

            return Ok(ApiResponse<TaskViewModel>.Ok(result, "Tasks reordered."));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.DeleteTaskAsync(userId, id);
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

            return Ok(ApiResponse<TaskAssigneeViewModel>.Ok(result, "Assignee added to task."));
        }

        [HttpPost("{taskId}/RemoveAssignee/{targetUserId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAssignee(Guid taskId, Guid targetUserId)
        {
            var userId = GetCurrentUserId();
            var result = await _tasksService.RemoveTaskAssigneeAsync(userId, taskId, targetUserId);
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
            if (!task.PrimaryAssigneeUserId.HasValue)
            {
                return;
            }

            await NotifyUserAsync(task.PrimaryAssigneeUserId.Value, currentUserId,
                "Task status updated",
                $"\"{task.Title}\" status changed to {task.Status}.",
                $"/Task/Detail/{task.Id}");
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
