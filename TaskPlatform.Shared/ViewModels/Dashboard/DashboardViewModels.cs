using System;
using System.Collections.Generic;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.Task;

namespace TaskPlatform.Shared.ViewModels.Dashboard
{
    public class DashboardOverviewViewModel
    {
        public Guid WorkspaceId { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRatePercentage { get; set; }
        public List<TaskViewModel> UpcomingTasks { get; set; } = new List<TaskViewModel>();
        public List<ActivityLogViewModel> RecentActivities { get; set; } = new List<ActivityLogViewModel>();
    }
}
