namespace TaskPlatform.Shared.ViewModels.TimeTracking
{
    public class TimeLogViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class ActiveTimerViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }

    public class StopTimerRequestViewModel
    {
        public string? Notes { get; set; }
    }

    public class UpdateTimeLogNotesRequestViewModel
    {
        public string? Notes { get; set; }
    }

    public class TimeTrackingReportRowViewModel
    {
        public Guid Id { get; set; }
        public string TeamMember { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class TimeTrackingReportViewModel
    {
        public double TotalHoursLogged { get; set; }
        public int ActiveUsersCurrentlyTracking { get; set; }
        public string MostActiveProject { get; set; } = "—";
        public List<TimeTrackingReportRowViewModel> Rows { get; set; } = new();
    }
}
