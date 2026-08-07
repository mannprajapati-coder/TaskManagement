using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Calendar;
using TaskPlatform.Shared.ViewModels.Common;

namespace Modules.Tasks.Domain.IServices
{
    public interface ICalendarService
    {
        Task<List<CalendarEventViewModel>> GetCalendarEventsAsync(Guid workspaceId, DateTime startDate, DateTime endDate);
        Task<ApiResponse<bool>> RescheduleTaskAsync(Guid userId, RescheduleTaskDateRequestViewModel model);
    }
}
