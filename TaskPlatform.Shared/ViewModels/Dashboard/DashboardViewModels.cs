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
        public double TaskGrowthPercentage { get; set; }
        public List<TaskViewModel> UpcomingTasks { get; set; } = new List<TaskViewModel>();
        public List<ActivityLogViewModel> RecentActivities { get; set; } = new List<ActivityLogViewModel>();
        public List<DailyCompletionViewModel> CompletionVelocity { get; set; } = new List<DailyCompletionViewModel>();
        public List<ProjectWorkloadViewModel> ProjectWorkload { get; set; } = new List<ProjectWorkloadViewModel>();
        public List<ProjectCompletionViewModel> ProjectCompletion { get; set; } = new List<ProjectCompletionViewModel>();
    }

    public class DailyCompletionViewModel
    {
        public string DayLabel { get; set; } = string.Empty;
        public int CompletedCount { get; set; }
    }

    public class ProjectWorkloadViewModel
    {
        public Guid ProjectId { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
    }

    public class ProjectCompletionViewModel
    {
        public Guid ProjectId { get; set; }
        public double CompletionPercentage { get; set; }
    }
}
