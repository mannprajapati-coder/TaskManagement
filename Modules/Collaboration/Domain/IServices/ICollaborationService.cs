using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Collaboration;

namespace Modules.Collaboration.Domain.IServices
{
    public interface ICollaborationService
    {
        Task<List<CommentViewModel>> GetTaskCommentsAsync(Guid taskId);
        Task<CommentViewModel> AddTaskCommentAsync(Guid userId, AddCommentRequestViewModel model);
        Task<bool> DeleteTaskCommentAsync(Guid userId, Guid commentId);

        Task<List<AttachmentViewModel>> GetTaskAttachmentsAsync(Guid taskId);
        Task<AttachmentViewModel> AddTaskAttachmentAsync(Guid userId, Guid taskId, string fileName, string filePath, long fileSize, string contentType);
        Task<bool> DeleteTaskAttachmentAsync(Guid userId, Guid attachmentId);
    }
}
