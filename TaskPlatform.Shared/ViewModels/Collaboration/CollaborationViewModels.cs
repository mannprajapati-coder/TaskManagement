using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.Collaboration
{
    // Comment ViewModels
    public class CommentViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public List<string> MentionedUserIds { get; set; } = new List<string>();
        public List<AttachmentMentionViewModel> MentionedAttachments { get; set; } = new List<AttachmentMentionViewModel>();
        public DateTime CreatedAt { get; set; }
    }

    public class AttachmentMentionViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class AddCommentRequestViewModel
    {
        [Required]
        public Guid TaskId { get; set; }

        [Required(ErrorMessage = "Comment text is required.")]
        [StringLength(2000, ErrorMessage = "Comment text cannot exceed 2000 characters.")]
        public string CommentText { get; set; } = string.Empty;

        public List<string>? MentionedUserIds { get; set; }
        public List<Guid>? MentionedAttachmentIds { get; set; }
    }

    // Attachment ViewModels
    public class AttachmentViewModel
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid UploadedByUserId { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
