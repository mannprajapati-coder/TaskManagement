using System;

namespace Modules.Tasks.Domain.Entities
{
    // Read-only projection of the Projects table (owned by the Projects module's migrations).
    // All modules share the same physical database, so this lets Tasks resolve which
    // workspace a task's project belongs to without a cross-module service call.
    public class ProjectLookup
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
    }
}
