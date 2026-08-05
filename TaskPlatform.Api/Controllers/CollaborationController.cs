using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Collaboration.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Collaboration;
using TaskPlatform.Shared.ViewModels.Common;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CollaborationController : ControllerBase
    {
        private readonly ICollaborationService _collaborationService;

        public CollaborationController(ICollaborationService collaborationService)
        {
            _collaborationService = collaborationService;
        }

        [HttpGet("Tasks/{taskId}/Comments")]
        public async Task<ActionResult<ApiResponse<List<CommentViewModel>>>> GetComments(Guid taskId)
        {
            var result = await _collaborationService.GetTaskCommentsAsync(taskId);
            return Ok(ApiResponse<List<CommentViewModel>>.Ok(result));
        }

        [HttpPost("Comments/Add")]
        public async Task<ActionResult<ApiResponse<CommentViewModel>>> AddComment([FromBody] AddCommentRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.AddTaskCommentAsync(userId, model);
            return Ok(ApiResponse<CommentViewModel>.Ok(result, "Comment added successfully."));
        }

        [HttpDelete("Comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.DeleteTaskCommentAsync(userId, commentId);
            return Ok(ApiResponse<bool>.Ok(result, "Comment deleted."));
        }

        [HttpGet("Tasks/{taskId}/Attachments")]
        public async Task<ActionResult<ApiResponse<List<AttachmentViewModel>>>> GetAttachments(Guid taskId)
        {
            var result = await _collaborationService.GetTaskAttachmentsAsync(taskId);
            return Ok(ApiResponse<List<AttachmentViewModel>>.Ok(result));
        }

        [HttpPost("Attachments/Add")]
        public async Task<ActionResult<ApiResponse<AttachmentViewModel>>> AddAttachment([FromQuery] Guid taskId, [FromQuery] string fileName, [FromQuery] string filePath, [FromQuery] long fileSize, [FromQuery] string contentType)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.AddTaskAttachmentAsync(userId, taskId, fileName, filePath, fileSize, contentType);
            return Ok(ApiResponse<AttachmentViewModel>.Ok(result, "Attachment uploaded successfully."));
        }

        [HttpDelete("Attachments/{attachmentId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAttachment(Guid attachmentId)
        {
            var userId = GetCurrentUserId();
            var result = await _collaborationService.DeleteTaskAttachmentAsync(userId, attachmentId);
            return Ok(ApiResponse<bool>.Ok(result, "Attachment deleted."));
        }

        private Guid GetCurrentUserId()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId;
        }
    }
}
