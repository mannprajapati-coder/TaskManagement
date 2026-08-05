using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Workspace
{
    public class WorkspaceViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerUserId { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateWorkspaceRequestViewModel
    {
        [Required(ErrorMessage = "Workspace Name is required.")]
        [StringLength(100, ErrorMessage = "Workspace Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }

    public class UpdateWorkspaceSettingsRequestViewModel
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required(ErrorMessage = "Workspace Name is required.")]
        [StringLength(100, ErrorMessage = "Workspace Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }

    public class InviteMembersRequestViewModel
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        public List<string> Emails { get; set; } = new List<string>();

        public int MaxUses { get; set; } = 10;
        public int ExpiryDays { get; set; } = 7;
    }

    public class WorkspaceInviteViewModel
    {
        public string InviteToken { get; set; } = string.Empty;
        public string WorkspaceName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int MaxUses { get; set; }
        public int UseCount { get; set; }
        public string InviteUrl { get; set; } = string.Empty;
    }

    public class JoinWorkspaceRequestViewModel
    {
        [Required(ErrorMessage = "Invite token is required.")]
        public string Token { get; set; } = string.Empty;
    }
}
