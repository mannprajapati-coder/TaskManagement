using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Collaboration.Domain.Entities;
using Modules.Collaboration.Domain.IServices;
using Modules.Collaboration.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Collaboration;

namespace Modules.Collaboration.Application.Services
{
    public class CollaborationService : ICollaborationService
    {
        private readonly CollaborationDbContext _dbContext;

        public CollaborationService(CollaborationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CommentViewModel>> GetTaskCommentsAsync(Guid taskId)
        {
            var comments = await _dbContext.TaskComments
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(comments.Select(c => c.UserId));

            var mentionedAttachmentIds = comments
                .SelectMany(c => DeserializeGuidList(c.MentionedAttachmentIdsJson))
                .Distinct()
                .ToList();
            var attachmentLookup = await _dbContext.TaskAttachments
                .Where(a => mentionedAttachmentIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.FileName);

            return comments.Select(c => new CommentViewModel
            {
                Id = c.Id,
                TaskId = c.TaskId,
                UserId = c.UserId,
                AuthorName = userLookup.TryGetValue(c.UserId, out var u) ? u.FullName : "Unknown User",
                CommentText = c.CommentText,
                MentionedUserIds = string.IsNullOrEmpty(c.MentionedUserIdsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(c.MentionedUserIdsJson) ?? new List<string>(),
                MentionedAttachments = DeserializeGuidList(c.MentionedAttachmentIdsJson)
                    .Where(id => attachmentLookup.ContainsKey(id))
                    .Select(id => new AttachmentMentionViewModel { Id = id, FileName = attachmentLookup[id] })
                    .ToList(),
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        private static List<Guid> DeserializeGuidList(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new List<Guid>();
            }
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }

        private async Task<Dictionary<Guid, UserLookup>> GetUserLookupAsync(IEnumerable<Guid> userIds)
        {
            var distinctIds = userIds.Distinct().ToList();
            return await _dbContext.UserLookups
                .Where(u => distinctIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        public async Task<CommentViewModel> AddTaskCommentAsync(Guid userId, AddCommentRequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CommentText))
            {
                throw new DomainException("Comment text cannot be empty.");
            }

            var mentionsJson = model.MentionedUserIds != null && model.MentionedUserIds.Any()
                ? JsonSerializer.Serialize(model.MentionedUserIds)
                : null;

            var attachmentMentionsJson = model.MentionedAttachmentIds != null && model.MentionedAttachmentIds.Any()
                ? JsonSerializer.Serialize(model.MentionedAttachmentIds)
                : null;

            var comment = new TaskComment
            {
                Id = Guid.NewGuid(),
                TaskId = model.TaskId,
                UserId = userId,
                CommentText = model.CommentText,
                MentionedUserIdsJson = mentionsJson,
                MentionedAttachmentIdsJson = attachmentMentionsJson,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.TaskComments.Add(comment);
            await _dbContext.SaveChangesAsync();

            var author = await _dbContext.UserLookups.FirstOrDefaultAsync(u => u.Id == userId);

            var mentionedAttachments = new List<AttachmentMentionViewModel>();
            if (model.MentionedAttachmentIds != null && model.MentionedAttachmentIds.Any())
            {
                var attachments = await _dbContext.TaskAttachments
                    .Where(a => model.MentionedAttachmentIds.Contains(a.Id))
                    .ToListAsync();
                mentionedAttachments = attachments
                    .Select(a => new AttachmentMentionViewModel { Id = a.Id, FileName = a.FileName })
                    .ToList();
            }

            return new CommentViewModel
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                AuthorName = author?.FullName ?? "Unknown User",
                CommentText = comment.CommentText,
                MentionedUserIds = model.MentionedUserIds ?? new List<string>(),
                MentionedAttachments = mentionedAttachments,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteTaskCommentAsync(Guid userId, Guid commentId)
        {
            var comment = await _dbContext.TaskComments.FindAsync(commentId);
            if (comment != null)
            {
                if (comment.UserId != userId)
                {
                    throw new DomainException("You can only delete your own comments.");
                }

                _dbContext.TaskComments.Remove(comment);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<List<AttachmentViewModel>> GetTaskAttachmentsAsync(Guid taskId)
        {
            var attachments = await _dbContext.TaskAttachments
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(attachments.Select(a => a.UploadedByUserId));

            return attachments.Select(a => new AttachmentViewModel
            {
                Id = a.Id,
                TaskId = a.TaskId,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                UploadedByUserId = a.UploadedByUserId,
                UploadedByName = userLookup.TryGetValue(a.UploadedByUserId, out var u) ? u.FullName : "Unknown User",
                UploadedAt = a.UploadedAt
            }).ToList();
        }

        public async Task<AttachmentViewModel> AddTaskAttachmentAsync(Guid userId, Guid taskId, string fileName, string filePath, long fileSize, string contentType)
        {
            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                ContentType = contentType,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            _dbContext.TaskAttachments.Add(attachment);
            await _dbContext.SaveChangesAsync();

            var uploader = await _dbContext.UserLookups.FirstOrDefaultAsync(u => u.Id == userId);

            return new AttachmentViewModel
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByName = uploader?.FullName ?? "Unknown User",
                UploadedAt = attachment.UploadedAt
            };
        }

        public async Task<bool> DeleteTaskAttachmentAsync(Guid userId, Guid attachmentId)
        {
            var attachment = await _dbContext.TaskAttachments.FindAsync(attachmentId);
            if (attachment != null)
            {
                _dbContext.TaskAttachments.Remove(attachment);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }
    }
}
