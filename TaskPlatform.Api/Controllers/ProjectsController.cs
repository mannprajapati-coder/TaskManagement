using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Projects.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Project;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectsService _projectsService;

        public ProjectsController(IProjectsService projectsService)
        {
            _projectsService = projectsService;
        }

        [HttpGet("GetByWorkspace/{workspaceId}")]
        public async Task<ActionResult<ApiResponse<List<ProjectViewModel>>>> GetByWorkspace(Guid workspaceId)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.GetWorkspaceProjectsAsync(workspaceId, userId);
            return Ok(ApiResponse<List<ProjectViewModel>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectViewModel>>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.GetProjectByIdAsync(id, userId);
            return Ok(ApiResponse<ProjectViewModel>.Ok(result));
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<ProjectViewModel>>> Create([FromBody] CreateProjectRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.CreateProjectAsync(userId, model);
            return Ok(ApiResponse<ProjectViewModel>.Ok(result, "Project created successfully."));
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<ProjectViewModel>>> Update([FromBody] UpdateProjectRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.UpdateProjectAsync(userId, model);
            return Ok(ApiResponse<ProjectViewModel>.Ok(result, "Project updated successfully."));
        }

        [HttpPost("Archive/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Archive(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.ArchiveProjectAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Project archived successfully."));
        }

        [HttpPost("Unarchive/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Unarchive(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.UnarchiveProjectAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Project unarchived successfully."));
        }

        [HttpPost("ToggleFavorite/{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleFavorite(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.ToggleFavoriteAsync(userId, id);
            return Ok(ApiResponse<bool>.Ok(result, "Project favorite toggled."));
        }

        [HttpGet("{projectId}/Members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberViewModel>>>> GetMembers(Guid projectId)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.GetProjectMembersAsync(projectId, userId);
            return Ok(ApiResponse<List<ProjectMemberViewModel>>.Ok(result));
        }

        [HttpPost("AddMember")]
        public async Task<ActionResult<ApiResponse<ProjectMemberViewModel>>> AddMember([FromBody] AddProjectMemberRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.AddMemberAsync(userId, model);
            return Ok(ApiResponse<ProjectMemberViewModel>.Ok(result, "Project member added successfully."));
        }

        [HttpPost("{projectId}/RemoveMember/{targetUserId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(Guid projectId, Guid targetUserId)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.RemoveMemberAsync(userId, projectId, targetUserId);
            return Ok(ApiResponse<bool>.Ok(result, "Project member removed."));
        }

        [HttpPut("{projectId}/UpdateMemberRole/{targetUserId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateMemberRole(Guid projectId, Guid targetUserId, [FromBody] string role)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.UpdateMemberRoleAsync(userId, projectId, targetUserId, role);
            return Ok(ApiResponse<bool>.Ok(result, "Member role updated."));
        }

        [HttpPost("SubmitJoinRequest/{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectJoinRequestViewModel>>> SubmitJoinRequest(Guid projectId)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.SubmitJoinRequestAsync(userId, projectId);
            return Ok(ApiResponse<ProjectJoinRequestViewModel>.Ok(result, "Join request submitted."));
        }

        [HttpGet("{projectId}/PendingJoinRequests")]
        public async Task<ActionResult<ApiResponse<List<ProjectJoinRequestViewModel>>>> GetPendingJoinRequests(Guid projectId)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.GetPendingJoinRequestsAsync(projectId, userId);
            return Ok(ApiResponse<List<ProjectJoinRequestViewModel>>.Ok(result));
        }

        [HttpPost("ResolveJoinRequest")]
        public async Task<ActionResult<ApiResponse<bool>>> ResolveJoinRequest([FromBody] ResolveJoinRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _projectsService.ResolveJoinRequestAsync(userId, model);
            return Ok(ApiResponse<bool>.Ok(result, "Join request resolved."));
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
