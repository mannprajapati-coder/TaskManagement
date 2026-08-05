using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace Modules.Workspaces.Domain.IServices
{
    public interface IWorkspaceService
    {
        Task<List<WorkspaceViewModel>> GetUserWorkspacesAsync(Guid userId);
        Task<WorkspaceViewModel> GetWorkspaceByIdAsync(Guid workspaceId, Guid userId);
        Task<WorkspaceViewModel> CreateWorkspaceAsync(Guid ownerUserId, CreateWorkspaceRequestViewModel model);
        Task<WorkspaceViewModel> UpdateWorkspaceSettingsAsync(Guid userId, UpdateWorkspaceSettingsRequestViewModel model);
        Task<bool> ArchiveWorkspaceAsync(Guid userId, Guid workspaceId);
        Task<bool> UnarchiveWorkspaceAsync(Guid userId, Guid workspaceId);
        Task<WorkspaceInviteViewModel> CreateInviteAsync(Guid userId, InviteMembersRequestViewModel model);
        Task<bool> JoinViaInviteAsync(Guid userId, string token);
    }
}
