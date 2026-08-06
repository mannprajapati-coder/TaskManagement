using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Notifications.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Notification;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("Activity/Workspace/{workspaceId}")]
        public async Task<ActionResult<ApiResponse<List<ActivityLogViewModel>>>> GetWorkspaceActivity(Guid workspaceId)
        {
            var result = await _notificationService.GetWorkspaceActivityAsync(workspaceId);
            return Ok(ApiResponse<List<ActivityLogViewModel>>.Ok(result));
        }

        [HttpGet("Activity/Project/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<ActivityLogViewModel>>>> GetProjectActivity(Guid projectId)
        {
            var result = await _notificationService.GetProjectActivityAsync(projectId);
            return Ok(ApiResponse<List<ActivityLogViewModel>>.Ok(result));
        }

        [HttpGet("Activity/Task/{taskId}")]
        public async Task<ActionResult<ApiResponse<List<ActivityLogViewModel>>>> GetTaskActivity(Guid taskId)
        {
            var result = await _notificationService.GetTaskActivityAsync(taskId);
            return Ok(ApiResponse<List<ActivityLogViewModel>>.Ok(result));
        }

        [HttpPost("Activity/Log")]
        public async Task<ActionResult<ApiResponse<bool>>> LogActivity([FromBody] CreateActivityLogRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.LogActivityAsync(userId, model);
            return Ok(ApiResponse<bool>.Ok(result, "Activity logged."));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationItemViewModel>>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(ApiResponse<List<NotificationItemViewModel>>.Ok(result));
        }

        [HttpPost("MarkAsRead/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAsReadAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Notification marked as read."));
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(ApiResponse<bool>.Ok(result, "All notifications marked as read."));
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
