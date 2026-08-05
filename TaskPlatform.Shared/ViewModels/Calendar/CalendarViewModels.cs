using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Calendar
{
    public class CalendarEventViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string? End { get; set; }
        public string Status { get; set; } = "Todo";
        public string Priority { get; set; } = "Medium";
        public string? Url { get; set; }
        public bool AllDay { get; set; } = true;
        public string Color { get; set; } = "#3b82f6";
    }

    public class RescheduleTaskDateRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        public DateTime? NewStartDate { get; set; }

        [Required]
        public DateTime NewDueDate { get; set; }
    }
}
