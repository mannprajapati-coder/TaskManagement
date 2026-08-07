namespace Modules.TimeTracking.Domain.Entities
{
    // Read-only cross-module projections onto tables owned by other modules.
    // Mapped via ExcludeFromMigrations() in TimeTrackingDbContext so this module never
    // creates/alters these tables — it only reads them.

    public class TaskLookup
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class ProjectLookup
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserLookup
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class WorkspaceLookup
    {
        public Guid Id { get; set; }
        public Guid OwnerUserId { get; set; }
    }
}
