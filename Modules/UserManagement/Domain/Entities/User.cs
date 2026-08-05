using System;
using Microsoft.AspNetCore.Identity;

namespace Modules.UserManagement.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? JobTitle { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsEmailVerified { get; set; }
        public string? GoogleSubjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
