using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Project
{
    public class ProjectViewModel
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? Client { get; set; }
        public Guid? ProjectManagerUserId { get; set; }
        public bool IsArchived { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateProjectRequestViewModel
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required(ErrorMessage = "Project Name is required.")]
        [StringLength(100, ErrorMessage = "Project Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? Client { get; set; }
    }

    public class UpdateProjectRequestViewModel
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required(ErrorMessage = "Project Name is required.")]
        [StringLength(100, ErrorMessage = "Project Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public string Status { get; set; } = "Active";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? Client { get; set; }
    }

    public class ProjectMemberViewModel
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Developer";
        public DateTime JoinedAt { get; set; }
    }

    public class AddProjectMemberRequestViewModel
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public string Role { get; set; } = "Developer";
    }

    public class ProjectJoinRequestViewModel
    {
        public Guid RequestId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public Guid RequestingUserId { get; set; }
        public string RequestingUserName { get; set; } = string.Empty;
        public string RequestingUserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
    }

    public class ResolveJoinRequestViewModel
    {
        [Required]
        public Guid RequestId { get; set; }

        public bool Approve { get; set; }
    }
}
