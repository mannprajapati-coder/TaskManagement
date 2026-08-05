using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Workspaces.Domain.Entities;
using Modules.Workspaces.Domain.IServices;
using Modules.Workspaces.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace Modules.Workspaces.Application.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly WorkspacesDbContext _dbContext;

        public WorkspaceService(WorkspacesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<WorkspaceViewModel>> GetUserWorkspacesAsync(Guid userId)
        {
            var workspaceIds = await _dbContext.WorkspaceMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.WorkspaceId)
                .ToListAsync();

            var workspaces = await _dbContext.Workspaces
                .Include(w => w.Members)
                .Where(w => workspaceIds.Contains(w.Id) && !w.IsArchived)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return workspaces.Select(MapToViewModel).ToList();
        }

        public async Task<WorkspaceViewModel> GetWorkspaceByIdAsync(Guid workspaceId, Guid userId)
        {
            var isMember = await _dbContext.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            if (!isMember)
            {
                throw new PermissionDeniedException("You are not a member of this workspace.");
            }

            var workspace = await _dbContext.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }

            return MapToViewModel(workspace);
        }

        public async Task<WorkspaceViewModel> CreateWorkspaceAsync(Guid ownerUserId, CreateWorkspaceRequestViewModel model)
        {
            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                OwnerUserId = ownerUserId,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };

            var ownerMember = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                UserId = ownerUserId,
                Role = "Owner",
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.Workspaces.Add(workspace);
            _dbContext.WorkspaceMembers.Add(ownerMember);

            await _dbContext.SaveChangesAsync();

            workspace.Members.Add(ownerMember);
            return MapToViewModel(workspace);
        }

        public async Task<WorkspaceViewModel> UpdateWorkspaceSettingsAsync(Guid userId, UpdateWorkspaceSettingsRequestViewModel model)
        {
            var workspace = await _dbContext.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId);

            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }

            if (workspace.OwnerUserId != userId)
            {
                throw new PermissionDeniedException("Only the workspace owner can update settings.");
            }

            workspace.Name = model.Name;
            workspace.Description = model.Description;

            await _dbContext.SaveChangesAsync();
            return MapToViewModel(workspace);
        }

        public async Task<bool> ArchiveWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var workspace = await _dbContext.Workspaces.FindAsync(workspaceId);
            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }

            if (workspace.OwnerUserId != userId)
            {
                throw new PermissionDeniedException("Only the workspace owner can archive a workspace.");
            }

            // BR-04-02: Soft archive workspace
            workspace.IsArchived = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnarchiveWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            var workspace = await _dbContext.Workspaces.FindAsync(workspaceId);
            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }

            if (workspace.OwnerUserId != userId)
            {
                throw new PermissionDeniedException("Only the workspace owner can unarchive a workspace.");
            }

            workspace.IsArchived = false;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<WorkspaceInviteViewModel> CreateInviteAsync(Guid userId, InviteMembersRequestViewModel model)
        {
            var isMember = await _dbContext.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == model.WorkspaceId && m.UserId == userId);

            if (!isMember)
            {
                throw new PermissionDeniedException("You must be a workspace member to create invite links.");
            }

            var workspace = await _dbContext.Workspaces.FindAsync(model.WorkspaceId);
            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }

            var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var tokenHash = HashString(rawToken);

            var invite = new WorkspaceInvite
            {
                Id = Guid.NewGuid(),
                WorkspaceId = model.WorkspaceId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(model.ExpiryDays > 0 ? model.ExpiryDays : 7),
                MaxUses = model.MaxUses > 0 ? model.MaxUses : 10,
                UseCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.WorkspaceInvites.Add(invite);
            await _dbContext.SaveChangesAsync();

            return new WorkspaceInviteViewModel
            {
                InviteToken = rawToken,
                WorkspaceName = workspace.Name,
                ExpiresAt = invite.ExpiresAt,
                MaxUses = invite.MaxUses,
                UseCount = invite.UseCount,
                InviteUrl = $"https://localhost:7203/Workspace/Join?token={rawToken}"
            };
        }

        public async Task<bool> JoinViaInviteAsync(Guid userId, string token)
        {
            var tokenHash = HashString(token);
            var invite = await _dbContext.WorkspaceInvites
                .Include(i => i.Workspace)
                .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

            if (invite == null)
            {
                throw new DomainException("Invalid invite link token.");
            }

            // BR-04-01: Expiry and max uses checks with specific error messages
            if (invite.ExpiresAt < DateTime.UtcNow)
            {
                throw new DomainException("Workspace invite link has expired.");
            }

            if (invite.UseCount >= invite.MaxUses)
            {
                throw new DomainException("Workspace invite link usage limit has been reached.");
            }

            var isAlreadyMember = await _dbContext.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == invite.WorkspaceId && m.UserId == userId);

            if (isAlreadyMember)
            {
                return true; // Already member
            }

            var member = new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                WorkspaceId = invite.WorkspaceId,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            invite.UseCount++;

            _dbContext.WorkspaceMembers.Add(member);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static WorkspaceViewModel MapToViewModel(Workspace workspace)
        {
            return new WorkspaceViewModel
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                OwnerUserId = workspace.OwnerUserId,
                IsArchived = workspace.IsArchived,
                CreatedAt = workspace.CreatedAt,
                MemberCount = workspace.Members?.Count ?? 0
            };
        }

        private static string HashString(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
