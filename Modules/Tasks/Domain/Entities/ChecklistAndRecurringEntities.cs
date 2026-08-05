using System;

namespace Modules.Tasks.Domain.Entities
{
    public class ChecklistItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TaskEntity? Task { get; set; }
    }

    public class RecurringTaskRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public string RecurrencePattern { get; set; } = "Daily"; // Daily, Weekly, Monthly
        public int Interval { get; set; } = 1;
        public string? DaysOfWeek { get; set; }
        public DateTime NextRunDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TaskEntity? Task { get; set; }
    }
}
