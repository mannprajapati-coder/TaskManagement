using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Workspaces.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspacesController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WorkspaceViewModel>>>> GetMyWorkspaces()
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.GetUserWorkspacesAsync(userId);
            return Ok(ApiResponse<List<WorkspaceViewModel>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<WorkspaceViewModel>>> GetWorkspaceById(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.GetWorkspaceByIdAsync(id, userId);
            return Ok(ApiResponse<WorkspaceViewModel>.Ok(result));
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<WorkspaceViewModel>>> Create([FromBody] CreateWorkspaceRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.CreateWorkspaceAsync(userId, model);
            return Ok(ApiResponse<WorkspaceViewModel>.Ok(result, "Workspace created successfully."));
        }

        [HttpPut("UpdateSettings")]
        public async Task<ActionResult<ApiResponse<WorkspaceViewModel>>> UpdateSettings([FromBody] UpdateWorkspaceSettingsRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.UpdateWorkspaceSettingsAsync(userId, model);
            return Ok(ApiResponse<WorkspaceViewModel>.Ok(result, "Workspace settings updated successfully."));
        }

        [HttpPost("Archive/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Archive(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.ArchiveWorkspaceAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Workspace archived successfully."));
        }

        [HttpPost("Unarchive/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Unarchive(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.UnarchiveWorkspaceAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Workspace unarchived successfully."));
        }

        [HttpPost("InviteMembers")]
        public async Task<ActionResult<ApiResponse<WorkspaceInviteViewModel>>> InviteMembers([FromBody] InviteMembersRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.CreateInviteAsync(userId, model);
            return Ok(ApiResponse<WorkspaceInviteViewModel>.Ok(result, "Invite link generated successfully."));
        }

        [HttpPost("JoinViaInvite/{token}")]
        public async Task<ActionResult<ApiResponse<bool>>> JoinViaInvite(string token)
        {
            var userId = GetCurrentUserId();
            var result = await _workspaceService.JoinViaInviteAsync(userId, token);
            return Ok(ApiResponse<bool>.Ok(result, "Joined workspace successfully."));
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
