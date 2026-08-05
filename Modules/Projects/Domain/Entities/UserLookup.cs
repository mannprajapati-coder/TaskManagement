using System;

namespace Modules.Projects.Domain.Entities
{
    // Read-only projection of the Users table (owned by the UserManagement module's migrations).
    // All modules share the same physical database, so this lets Projects resolve real
    // display names without a cross-module service call.
    public class UserLookup
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
