using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Task
{
    public class TaskViewModel
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Todo"; // Todo, InProgress, InReview, Completed, Cancelled
        public int SortOrder { get; set; }
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public Guid? PrimaryAssigneeUserId { get; set; }
        public string? PrimaryAssigneeName { get; set; }
        public Guid CreatorUserId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTaskRequestViewModel
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Task Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        public string Priority { get; set; } = "Medium";
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000.")]
        public decimal? EstimatedHours { get; set; }

        public Guid? PrimaryAssigneeUserId { get; set; }
    }

    public class UpdateTaskRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required(ErrorMessage = "Task Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        public string Priority { get; set; } = "Medium";
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        [Range(0, 1000, ErrorMessage = "Estimated hours must be between 0 and 1000.")]
        public decimal? EstimatedHours { get; set; }

        [Range(0, 1000, ErrorMessage = "Actual hours must be non-negative.")]
        public decimal? ActualHours { get; set; }

        public Guid? PrimaryAssigneeUserId { get; set; }
    }

    public class UpdateTaskStatusRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public string Status { get; set; } = "Todo";
    }

    public class ReorderTasksRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public string Status { get; set; } = "Todo";

        [Required]
        public List<Guid> OrderedTaskIds { get; set; } = new();
    }
}
