using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Tasks.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Calendar;
using TaskPlatform.Shared.ViewModels.Common;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;

        public CalendarController(ICalendarService calendarService)
        {
            _calendarService = calendarService;
        }

        [HttpGet("Events/{workspaceId}")]
        public async Task<ActionResult<ApiResponse<List<CalendarEventViewModel>>>> GetEvents(Guid workspaceId, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var startDate = start ?? DateTime.UtcNow.AddDays(-30);
            var endDate = end ?? DateTime.UtcNow.AddDays(90);

            var result = await _calendarService.GetCalendarEventsAsync(workspaceId, startDate, endDate);
            return Ok(ApiResponse<List<CalendarEventViewModel>>.Ok(result));
        }

        [HttpPost("Reschedule")]
        public async Task<ActionResult<ApiResponse<bool>>> Reschedule([FromBody] RescheduleTaskDateRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var response = await _calendarService.RescheduleTaskAsync(userId, model);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        private Guid GetCurrentUserId()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
            {
                return Guid.Empty;
            }
            return userId;
        }
    }
}
