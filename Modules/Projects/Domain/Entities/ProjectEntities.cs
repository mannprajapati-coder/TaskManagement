using System;
using System.Collections.Generic;

namespace Modules.Projects.Domain.Entities
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active"; // Active, Planning, OnHold, Completed, Archived
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? Client { get; set; }
        public Guid? ProjectManagerUserId { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<ProjectFavorite> Favorites { get; set; } = new List<ProjectFavorite>();
        public ICollection<ProjectJoinRequest> JoinRequests { get; set; } = new List<ProjectJoinRequest>();
    }

    public class ProjectFavorite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Project? Project { get; set; }
    }

    public class ProjectMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string ProjectScopedRole { get; set; } = "Developer"; // Owner, ProjectManager, SeniorDeveloper, Developer, QA
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Project? Project { get; set; }
    }

    public class ProjectJoinRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Guid RequestingUserId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public Guid? ResolvedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Project? Project { get; set; }
    }
}
