using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Notifications.Domain.IServices;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Dashboard;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly TasksDbContext _dbContext;
        private readonly INotificationService _notificationService;

        public DashboardService(TasksDbContext dbContext, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
        }

        public async Task<DashboardOverviewViewModel> GetWorkspaceDashboardOverviewAsync(Guid workspaceId, Guid userId)
        {
            var now = DateTime.UtcNow;

            var projectIds = await _dbContext.ProjectLookups
                .Where(p => p.WorkspaceId == workspaceId)
                .Select(p => p.Id)
                .ToListAsync();

            var tasks = await _dbContext.Tasks
                .Where(t => projectIds.Contains(t.ProjectId))
                .ToListAsync();

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

            var weekAgo = now.AddDays(-7);
            var twoWeeksAgo = now.AddDays(-14);
            var tasksThisWeek = tasks.Count(t => t.CreatedAt >= weekAgo);
            var tasksPriorWeek = tasks.Count(t => t.CreatedAt >= twoWeeksAgo && t.CreatedAt < weekAgo);
            var taskGrowthPercentage = tasksPriorWeek > 0
                ? Math.Round((double)(tasksThisWeek - tasksPriorWeek) / tasksPriorWeek * 100, 1)
                : (tasksThisWeek > 0 ? 100 : 0);

            var completionVelocity = new List<DailyCompletionViewModel>();
            for (var i = 6; i >= 0; i--)
            {
                var day = now.Date.AddDays(-i);
                completionVelocity.Add(new DailyCompletionViewModel
                {
                    DayLabel = day.ToString("ddd"),
                    CompletedCount = tasks.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value.Date == day)
                });
            }

            var projectWorkload = tasks
                .GroupBy(t => t.ProjectId)
                .Select(g => new ProjectWorkloadViewModel
                {
                    ProjectId = g.Key,
                    EstimatedHours = g.Sum(t => t.EstimatedHours ?? 0),
                    ActualHours = g.Sum(t => t.ActualHours ?? 0)
                })
                .OrderByDescending(w => w.EstimatedHours)
                .Take(5)
                .ToList();

            var projectCompletion = tasks
                .GroupBy(t => t.ProjectId)
                .Select(g => new ProjectCompletionViewModel
                {
                    ProjectId = g.Key,
                    CompletionPercentage = g.Count() > 0
                        ? Math.Round(100.0 * g.Count(t => t.Status == "Completed") / g.Count(), 1)
                        : 0
                })
                .ToList();

            var recentActivities = await _notificationService.GetWorkspaceActivityAsync(workspaceId);

            return new DashboardOverviewViewModel
            {
                WorkspaceId = workspaceId,
                TotalProjects = projectIds.Count,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,
                CompletionRatePercentage = completionRate,
                TaskGrowthPercentage = taskGrowthPercentage,
                UpcomingTasks = upcomingTasks,
                RecentActivities = recentActivities.Take(8).ToList(),
                CompletionVelocity = completionVelocity,
                ProjectWorkload = projectWorkload,
                ProjectCompletion = projectCompletion
            };
        }
    }
}
