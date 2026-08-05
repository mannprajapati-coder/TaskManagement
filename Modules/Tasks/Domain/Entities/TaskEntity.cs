using System;
using System.Collections.Generic;

namespace Modules.Tasks.Domain.Entities
{
    public class TaskEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
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
        public Guid CreatorUserId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Sprint 05: Subtask self-reference
        public Guid? ParentTaskId { get; set; }
        public TaskEntity? ParentTask { get; set; }
        public ICollection<TaskEntity> Subtasks { get; set; } = new List<TaskEntity>();

        // Sprint 05: Assignees & Watchers
        public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
        public ICollection<TaskWatcher> Watchers { get; set; } = new List<TaskWatcher>();
    }

    public class TaskAssignee
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public TaskEntity? Task { get; set; }
    }

    public class TaskWatcher
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public DateTime WatchingSince { get; set; } = DateTime.UtcNow;

        public TaskEntity? Task { get; set; }
    }
}
