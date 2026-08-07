using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Collaboration.Domain.IServices;
using Modules.Notifications.Domain.IServices;
using Modules.Tasks.Domain.IServices;
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
        private readonly ICollaborationService _collaborationService;

        public TasksController(
            ITasksService tasksService,
            INotificationService notificationService,
            ICollaborationService collaborationService)
        {
            _tasksService = tasksService;
            _notificationService = notificationService;
            _collaborationService = collaborationService;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
        }

        [HttpGet("Project/{projectId}")]
        [HttpGet("GetByProject/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<TaskViewModel>>>> GetProjectTasks(Guid projectId, [FromQuery] string? status, [FromQuery] string? priority)
        {
            var result = await _tasksService.GetProjectTasksAsync(projectId, status, priority);
            return Ok(ApiResponse<List<TaskViewModel>>.Ok(result));
        }

        [HttpGet("MyTasks")]
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
            var response = await _tasksService.GetTaskByIdAsync(id);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Create([FromBody] CreateTaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var response = await _tasksService.CreateTaskAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            await LogActivityAsync(userId, result.ProjectId, result.Id, "TaskCreated", $"Created task \"{result.Title}\".");
            return Ok(response);
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Update([FromBody] UpdateTaskRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var beforeResp = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var response = await _tasksService.UpdateTaskAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            if (beforeResp.Success && beforeResp.Data != null && beforeResp.Data.DueDate != result.DueDate)
            {
                await NotifyDueDateChangeAsync(result, userId);
            }

            await LogActivityAsync(userId, result.ProjectId, result.Id, "TaskUpdated", $"Updated task \"{result.Title}\".");
            return Ok(response);
        }

        [HttpPut("UpdateStatus")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> UpdateStatus([FromBody] UpdateTaskStatusRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var beforeResp = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var response = await _tasksService.UpdateTaskStatusAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            if (beforeResp.Success && beforeResp.Data != null && beforeResp.Data.Status != result.Status)
            {
                await NotifyStatusChangeAsync(result, userId);
                await LogActivityAsync(userId, result.ProjectId, result.Id, "StatusChanged", $"Status changed from {beforeResp.Data.Status} to {result.Status}.");
            }

            return Ok(response);
        }

        [HttpPut("Reorder")]
        public async Task<ActionResult<ApiResponse<TaskViewModel>>> Reorder([FromBody] ReorderTasksRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var beforeResp = await _tasksService.GetTaskByIdAsync(model.TaskId);
            var response = await _tasksService.ReorderTasksAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            if (beforeResp.Success && beforeResp.Data != null && beforeResp.Data.Status != result.Status)
            {
                await NotifyStatusChangeAsync(result, userId);
                await LogActivityAsync(userId, result.ProjectId, result.Id, "StatusChanged", $"Status changed from {beforeResp.Data.Status} to {result.Status}.");
            }

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var taskResp = await _tasksService.GetTaskByIdAsync(id);
            var response = await _tasksService.DeleteTaskAsync(userId, id);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            if (taskResp.Success && taskResp.Data != null)
            {
                await LogActivityAsync(userId, taskResp.Data.ProjectId, null, "TaskDeleted", $"Deleted task \"{taskResp.Data.Title}\".");
            }
            return Ok(response);
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
            var response = await _tasksService.CreateSubtaskAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            await LogActivityAsync(userId, result.ProjectId, result.ParentTaskId, "SubtaskCreated", $"Added subtask \"{result.Title}\".");
            return Ok(response);
        }

        [HttpDelete("{parentTaskId}/DeleteWithSubtasks")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteWithSubtasks(Guid parentTaskId)
        {
            var userId = GetCurrentUserId();
            var response = await _tasksService.DeleteTaskWithSubtasksAsync(userId, parentTaskId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
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
            var response = await _tasksService.AddTaskAssigneeAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            var taskResp = await _tasksService.GetTaskByIdAsync(model.TaskId);
            if (taskResp.Success && taskResp.Data != null)
            {
                await NotifyUserAsync(model.UserId, userId,
                    "You've been assigned a task",
                    $"\"{taskResp.Data.Title}\" was assigned to you.",
                    $"/Task/Detail/{taskResp.Data.Id}");
                await LogActivityAsync(userId, taskResp.Data.ProjectId, taskResp.Data.Id, "AssigneeAdded", $"Assigned {result.FullName} to \"{taskResp.Data.Title}\".");
            }

            return Ok(response);
        }

        [HttpPost("{taskId}/RemoveAssignee/{targetUserId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAssignee(Guid taskId, Guid targetUserId)
        {
            var userId = GetCurrentUserId();
            var taskResp = await _tasksService.GetTaskByIdAsync(taskId);
            var response = await _tasksService.RemoveTaskAssigneeAsync(userId, taskId, targetUserId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            if (taskResp.Success && taskResp.Data != null)
            {
                await LogActivityAsync(userId, taskResp.Data.ProjectId, taskResp.Data.Id, "AssigneeRemoved", $"Removed an assignee from \"{taskResp.Data.Title}\".");
            }
            return Ok(response);
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
            var response = await _tasksService.ToggleTaskWatcherAsync(userId, taskId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
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
            var response = await _tasksService.AddChecklistItemAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            var taskResp = await _tasksService.GetTaskByIdAsync(result.TaskId);
            if (taskResp.Success && taskResp.Data != null)
            {
                await LogActivityAsync(userId, taskResp.Data.ProjectId, taskResp.Data.Id, "ChecklistItemAdded", $"Added checklist item \"{result.Title}\".");
            }
            return Ok(response);
        }

        [HttpPost("ToggleChecklistItem/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleChecklistItem(Guid itemId)
        {
            var userId = GetCurrentUserId();
            var response = await _tasksService.ToggleChecklistItemAsync(userId, itemId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("DeleteChecklistItem/{itemId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteChecklistItem(Guid itemId)
        {
            var userId = GetCurrentUserId();
            var response = await _tasksService.DeleteChecklistItemAsync(userId, itemId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // Recurring Task Endpoints
        [HttpPost("SetRecurringRule")]
        public async Task<ActionResult<ApiResponse<RecurringTaskRuleViewModel>>> SetRecurringRule([FromBody] SetRecurringTaskRuleRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var response = await _tasksService.SetRecurringTaskRuleAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            var result = response.Data!;
            var taskResp = await _tasksService.GetTaskByIdAsync(result.TaskId);
            if (taskResp.Success && taskResp.Data != null)
            {
                await LogActivityAsync(userId, taskResp.Data.ProjectId, taskResp.Data.Id, "RecurringRuleSet", $"Set recurring rule ({result.RecurrencePattern}) on \"{taskResp.Data.Title}\".");
            }
            return Ok(response);
        }

        [HttpGet("{taskId}/RecurringRule")]
        public async Task<ActionResult<ApiResponse<RecurringTaskRuleViewModel>>> GetRecurringRule(Guid taskId)
        {
            var result = await _tasksService.GetRecurringTaskRuleAsync(taskId);
            if (result == null)
            {
                return ApiResponse<RecurringTaskRuleViewModel>.Fail("Recurring rule not found.");
            }
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
            foreach (var recipientId in recipients.Where(r => r != currentUserId))
            {
                await NotifyUserAsync(recipientId, currentUserId,
                    "Task status changed",
                    $"\"{task.Title}\" changed status to {task.Status}.",
                    $"/Task/Detail/{task.Id}");
            }
        }

        private async Task NotifyDueDateChangeAsync(TaskViewModel task, Guid currentUserId)
        {
            if (!task.DueDate.HasValue) return;

            var recipients = await GetNotificationRecipientsAsync(task);
            var dateStr = task.DueDate.Value.ToString("MMM dd, yyyy");
            foreach (var recipientId in recipients.Where(r => r != currentUserId))
            {
                await NotifyUserAsync(recipientId, currentUserId,
                    "Task due date changed",
                    $"Due date for \"{task.Title}\" was updated to {dateStr}.",
                    $"/Task/Detail/{task.Id}");
            }
        }

        private async Task<List<Guid>> GetNotificationRecipientsAsync(TaskViewModel task)
        {
            var recipients = new HashSet<Guid>();

            if (task.PrimaryAssigneeUserId.HasValue)
            {
                recipients.Add(task.PrimaryAssigneeUserId.Value);
            }

            var assignees = await _tasksService.GetTaskAssigneesAsync(task.Id);
            foreach (var a in assignees)
            {
                recipients.Add(a.UserId);
            }

            var watchers = await _tasksService.GetTaskWatchersAsync(task.Id);
            foreach (var w in watchers)
            {
                recipients.Add(w.UserId);
            }

            return recipients.ToList();
        }

        private async Task NotifyUserAsync(Guid targetUserId, Guid currentUserId, string title, string message, string linkUrl)
        {
            if (targetUserId == currentUserId) return;
            try
            {
                await _notificationService.SendNotificationAsync(new SendNotificationRequestViewModel
                {
                    UserId = targetUserId,
                    Title = title,
                    Message = message,
                    LinkUrl = linkUrl
                });
            }
            catch
            {
                // Silently swallow notification dispatch errors
            }
        }

        private async Task LogActivityAsync(Guid userId, Guid projectId, Guid? taskId, string action, string details)
        {
            try
            {
                await _notificationService.LogActivityAsync(userId, new CreateActivityLogRequestViewModel
                {
                    ProjectId = projectId,
                    TaskId = taskId,
                    Action = action,
                    Details = details
                });
            }
            catch
            {
                // Silently swallow activity log errors
            }
        }
    }
}
