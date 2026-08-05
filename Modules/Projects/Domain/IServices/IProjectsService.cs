using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Project;

namespace Modules.Projects.Domain.IServices
{
    public interface IProjectsService
    {
        Task<List<ProjectViewModel>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId);
        Task<ProjectViewModel> GetProjectByIdAsync(Guid projectId, Guid userId);
        Task<ProjectViewModel> CreateProjectAsync(Guid userId, CreateProjectRequestViewModel model);
        Task<ProjectViewModel> UpdateProjectAsync(Guid userId, UpdateProjectRequestViewModel model);
        Task<bool> ArchiveProjectAsync(Guid userId, Guid projectId);
        Task<bool> UnarchiveProjectAsync(Guid userId, Guid projectId);
        Task<bool> ToggleFavoriteAsync(Guid userId, Guid projectId);
        Task<List<ProjectMemberViewModel>> GetProjectMembersAsync(Guid projectId, Guid userId);
        Task<ProjectMemberViewModel> AddMemberAsync(Guid userId, AddProjectMemberRequestViewModel model);
        Task<bool> RemoveMemberAsync(Guid userId, Guid projectId, Guid targetUserId);
        Task<bool> UpdateMemberRoleAsync(Guid userId, Guid projectId, Guid targetUserId, string role);
        Task<ProjectJoinRequestViewModel> SubmitJoinRequestAsync(Guid userId, Guid projectId);
        Task<List<ProjectJoinRequestViewModel>> GetPendingJoinRequestsAsync(Guid projectId, Guid userId);
        Task<bool> ResolveJoinRequestAsync(Guid userId, ResolveJoinRequestViewModel model);
    }
}
