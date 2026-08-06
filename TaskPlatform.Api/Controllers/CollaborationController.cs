using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Modules.Collaboration.Domain.IServices;
using Modules.Notifications.Domain.IServices;
using Modules.Projects.Domain.IServices;
using Modules.Tasks.Domain.IServices;
using TaskPlatform.Api.Hubs;
using TaskPlatform.Shared.ViewModels.Collaboration;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.Task;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CollaborationController : ControllerBase
    {
        private readonly ICollaborationService _collaborationService;
        private readonly ITasksService _tasksService;
        private readonly IProjectsService _projectsService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public CollaborationController(ICollaborationService collaborationService, ITasksService tasksService, IProjectsService projectsService, INotificationService notificationService, IHubContext<NotificationsHub> hubContext)
        {
            _collaborationService = collaborationService;
            _tasksService = tasksService;
            _projectsService = projectsService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        [HttpGet("Tasks/{taskId}/Comments")]
        public async Task<ActionResult<ApiResponse<List<CommentViewModel>>>> GetComments(Guid taskId)
        {
            var result = await _collaborationService.GetTaskCommentsAsync(taskId);
            return Ok(ApiResponse<List<CommentViewModel>>.Ok(result));
        }

        [HttpPost("Comments/Add")]
        public async Task<ActionResult<ApiResponse<CommentViewModel>>> AddComment([FromBody] AddCommentRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.AddTaskCommentAsync(userId, model);
            var task = await _tasksService.GetTaskByIdAsync(model.TaskId);
            await LogActivityAsync(userId, task.ProjectId, task.Id, "CommentAdded", $"Commented on \"{task.Title}\".");
            await NotifyCommentAddedAsync(task, userId);
            return Ok(ApiResponse<CommentViewModel>.Ok(result, "Comment added successfully."));
        }

        [HttpDelete("Comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.DeleteTaskCommentAsync(userId, commentId);
            return Ok(ApiResponse<bool>.Ok(result, "Comment deleted."));
        }

        [HttpGet("Tasks/{taskId}/Attachments")]
        public async Task<ActionResult<ApiResponse<List<AttachmentViewModel>>>> GetAttachments(Guid taskId)
        {
            var result = await _collaborationService.GetTaskAttachmentsAsync(taskId);
            return Ok(ApiResponse<List<AttachmentViewModel>>.Ok(result));
        }

        [HttpPost("Attachments/Add")]
        public async Task<ActionResult<ApiResponse<AttachmentViewModel>>> AddAttachment([FromQuery] Guid taskId, [FromQuery] string fileName, [FromQuery] string filePath, [FromQuery] long fileSize, [FromQuery] string contentType)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.AddTaskAttachmentAsync(userId, taskId, fileName, filePath, fileSize, contentType);
            var task = await _tasksService.GetTaskByIdAsync(taskId);
            await LogActivityAsync(userId, task.ProjectId, task.Id, "AttachmentAdded", $"Attached \"{fileName}\" to \"{task.Title}\".");
            return Ok(ApiResponse<AttachmentViewModel>.Ok(result, "Attachment uploaded successfully."));
        }

        [HttpDelete("Attachments/{attachmentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAttachment(Guid attachmentId)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.DeleteTaskAttachmentAsync(userId, attachmentId);
            return Ok(ApiResponse<bool>.Ok(result, "Attachment deleted."));
        }

        // BR-12-02: Watchers are notified of Status changes, new Comments, and due-date changes only.
        private async Task NotifyCommentAddedAsync(TaskViewModel task, Guid currentUserId)
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

            foreach (var recipientId in recipients)
            {
                await NotifyUserAsync(recipientId, currentUserId,
                    "New comment on task",
                    $"New comment on \"{task.Title}\".",
                    $"/Task/Detail/{task.Id}");
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
                // Activity logging is best-effort and must never fail the primary operation.
            }
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
