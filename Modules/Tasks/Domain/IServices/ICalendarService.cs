using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Calendar;

namespace Modules.Tasks.Domain.IServices
{
    public interface ICalendarService
    {
        Task<List<CalendarEventViewModel>> GetCalendarEventsAsync(Guid workspaceId, DateTime startDate, DateTime endDate);
        Task<bool> RescheduleTaskAsync(Guid userId, RescheduleTaskDateRequestViewModel model);
    }
}
