using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Task
{
    // Subtask ViewModels
    public class SubtaskViewModel
    {
        public Guid Id { get; set; }
        public Guid ParentTaskId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "Todo";
        public string Priority { get; set; } = "Medium";
        public DateTime? DueDate { get; set; }
        public Guid? PrimaryAssigneeUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSubtaskRequestViewModel
    {
        [Required]
        public Guid ParentTaskId { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Subtask Title is required.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public string Priority { get; set; } = "Medium";
        public DateTime? DueDate { get; set; }
        public Guid? PrimaryAssigneeUserId { get; set; }
    }

    // Assignee ViewModels
    public class TaskAssigneeViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    public class AddTaskAssigneeRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public bool IsPrimary { get; set; }
    }

    // Watcher ViewModels
    public class TaskWatcherViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime WatchingSince { get; set; }
    }
}
