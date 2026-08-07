using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Modules.Notifications.Domain.IServices;
using Modules.TimeTracking.Domain.IServices;
using TaskPlatform.Api.Hubs;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.TimeTracking;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TimeTrackingController : ControllerBase
    {
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public TimeTrackingController(
            ITimeTrackingService timeTrackingService,
            INotificationService notificationService,
            IHubContext<NotificationsHub> hubContext)
        {
            _timeTrackingService = timeTrackingService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        [HttpPost("StartTimer/{taskId}")]
        public async Task<ActionResult<ApiResponse<TimeLogViewModel>>> StartTimer(Guid taskId)
        {
            var userId = GetCurrentUserId();
            var result = await _timeTrackingService.StartTimerAsync(userId, taskId);
            await LogActivityAsync(userId, result.TaskId, "TimerStarted", $"Started a timer on \"{result.TaskTitle}\".");
            await PushTimerChangedAsync(userId);
            return Ok(ApiResponse<TimeLogViewModel>.Ok(result, "Timer started."));
        }

        [HttpPost("StopTimer")]
        public async Task<ActionResult<ApiResponse<TimeLogViewModel>>> StopTimer([FromBody] StopTimerRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _timeTrackingService.StopTimerAsync(userId, model.Notes);
            await LogActivityAsync(userId, result.TaskId, "TimerStopped", $"Logged {result.DurationMinutes ?? 0} min on \"{result.TaskTitle}\".");
            await PushTimerChangedAsync(userId);
            return Ok(ApiResponse<TimeLogViewModel>.Ok(result, "Timer stopped."));
        }

        [HttpGet("GetActiveTimer")]
        public async Task<ActionResult<ApiResponse<ActiveTimerViewModel?>>> GetActiveTimer()
        {
            var userId = GetCurrentUserId();
            var result = await _timeTrackingService.GetActiveTimerAsync(userId);
            return Ok(ApiResponse<ActiveTimerViewModel?>.Ok(result));
        }

        [HttpGet("TaskLogs/{taskId}")]
        public async Task<ActionResult<ApiResponse<List<TimeLogViewModel>>>> GetTaskTimeLogs(Guid taskId)
        {
            var result = await _timeTrackingService.GetTaskTimeLogsAsync(taskId);
            return Ok(ApiResponse<List<TimeLogViewModel>>.Ok(result));
        }

        [HttpPut("UpdateNotes/{timeLogId}")]
        public async Task<ActionResult<ApiResponse<TimeLogViewModel>>> UpdateNotes(Guid timeLogId, [FromBody] UpdateTimeLogNotesRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _timeTrackingService.UpdateNotesAsync(userId, timeLogId, model.Notes);
            return Ok(ApiResponse<TimeLogViewModel>.Ok(result, "Notes updated."));
        }

        [HttpGet("Report")]
        public async Task<ActionResult<ApiResponse<TimeTrackingReportViewModel>>> GetReport(Guid workspaceId, DateTime from, DateTime to)
        {
            var userId = GetCurrentUserId();
            var result = await _timeTrackingService.GetReportAsync(userId, workspaceId, from, to);
            return Ok(ApiResponse<TimeTrackingReportViewModel>.Ok(result));
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

        private async Task LogActivityAsync(Guid userId, Guid taskId, string action, string details)
        {
            try
            {
                await _notificationService.LogActivityAsync(userId, new CreateActivityLogRequestViewModel
                {
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

        private async Task PushTimerChangedAsync(Guid userId)
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("TimerChanged");
            }
            catch
            {
                // Silently swallow push errors — the client falls back to its own REST seed.
            }
        }
    }
}
