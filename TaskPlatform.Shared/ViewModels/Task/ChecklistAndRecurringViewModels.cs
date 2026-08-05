using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Task
{
    // Checklist ViewModels
    public class ChecklistItemViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddChecklistItemRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required(ErrorMessage = "Checklist item title is required.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
    }

    // Recurring Task ViewModels
    public class RecurringTaskRuleViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string RecurrencePattern { get; set; } = "Daily"; // Daily, Weekly, Monthly
        public int Interval { get; set; } = 1;
        public string? DaysOfWeek { get; set; }
        public DateTime NextRunDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SetRecurringTaskRuleRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public string RecurrencePattern { get; set; } = "Daily";

        [Range(1, 100)]
        public int Interval { get; set; } = 1;

        public string? DaysOfWeek { get; set; }
        public DateTime StartRunDate { get; set; } = DateTime.UtcNow.AddDays(1);
    }
}
