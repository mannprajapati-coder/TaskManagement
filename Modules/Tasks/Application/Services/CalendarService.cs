using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Calendar;
using TaskPlatform.Shared.ViewModels.Common;

namespace Modules.Tasks.Application.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly TasksDbContext _dbContext;

        public CalendarService(TasksDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CalendarEventViewModel>> GetCalendarEventsAsync(Guid workspaceId, DateTime startDate, DateTime endDate)
        {
            var projectIds = await _dbContext.ProjectLookups
                .Where(p => p.WorkspaceId == workspaceId)
                .Select(p => p.Id)
                .ToListAsync();

            var tasks = await _dbContext.Tasks
                .Where(t => projectIds.Contains(t.ProjectId) &&
                            t.DueDate.HasValue && t.DueDate >= startDate && t.DueDate <= endDate)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(t => new CalendarEventViewModel
            {
                Id = t.Id.ToString(),
                Title = t.Title,
                Start = (t.StartDate ?? t.DueDate!.Value).ToString("yyyy-MM-dd"),
                End = t.DueDate!.Value.ToString("yyyy-MM-dd"),
                Status = t.Status,
                Priority = t.Priority,
                Url = $"/Task/Detail/{t.Id}",
                AllDay = true,
                Color = t.Priority switch
                {
                    "Urgent" => "#ef4444",
                    "High" => "#f59e0b",
                    "Medium" => "#3b82f6",
                    _ => "#6b7280"
                }
            }).ToList();
        }

        public async Task<ApiResponse<bool>> RescheduleTaskAsync(Guid userId, RescheduleTaskDateRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<bool>.Fail("Task not found.");
            }

            if (model.NewStartDate.HasValue && model.NewDueDate < model.NewStartDate.Value)
            {
                return ApiResponse<bool>.Fail("New due date must be on or after start date.");
            }

            task.StartDate = model.NewStartDate ?? task.StartDate;
            task.DueDate = model.NewDueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Task rescheduled successfully.");
        }
    }
}
