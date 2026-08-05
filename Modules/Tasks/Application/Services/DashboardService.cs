using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Dashboard;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly TasksDbContext _dbContext;

        public DashboardService(TasksDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardOverviewViewModel> GetWorkspaceDashboardOverviewAsync(Guid workspaceId, Guid userId)
        {
            var now = DateTime.UtcNow;

            var tasks = await _dbContext.Tasks.ToListAsync();

            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == "Completed");
            var inProgressTasks = tasks.Count(t => t.Status == "InProgress");
            var pendingTasks = tasks.Count(t => t.Status == "Todo" || t.Status == "InReview");
            var overdueTasks = tasks.Count(t => t.Status != "Completed" && t.DueDate.HasValue && t.DueDate < now);

            double completionRate = totalTasks > 0 ? Math.Round((double)completedTasks / totalTasks * 100, 1) : 0;

            var upcomingTasks = tasks
                .Where(t => t.Status != "Completed" && t.DueDate.HasValue)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new TaskViewModel
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    EstimatedHours = t.EstimatedHours,
                    ActualHours = t.ActualHours
                })
                .ToList();

            return new DashboardOverviewViewModel
            {
                WorkspaceId = workspaceId,
                TotalProjects = 1,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,
                CompletionRatePercentage = completionRate,
                UpcomingTasks = upcomingTasks,
                RecentActivities = new List<TaskPlatform.Shared.ViewModels.Notification.ActivityLogViewModel>()
            };
        }
    }
}
