using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Projects.Domain.Entities;
using Modules.Projects.Domain.IServices;
using Modules.Projects.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Project;

namespace Modules.Projects.Application.Services
{
    public class ProjectsService : IProjectsService
    {
        private readonly ProjectsDbContext _dbContext;

        public ProjectsService(ProjectsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProjectViewModel>> GetWorkspaceProjectsAsync(Guid workspaceId, Guid userId)
        {
            var userFavorites = await _dbContext.ProjectFavorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProjectId)
                .ToListAsync();

            var projects = await _dbContext.Projects
                .Include(p => p.Members)
                .Where(p => p.WorkspaceId == workspaceId && !p.IsArchived)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return projects.Select(p => MapToViewModel(p, userFavorites.Contains(p.Id))).ToList();
        }

        public async Task<ProjectViewModel> GetProjectByIdAsync(Guid projectId, Guid userId)
        {
            var project = await _dbContext.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new DomainException("Project not found.");
            }

            var isFavorite = await _dbContext.ProjectFavorites
                .AnyAsync(f => f.ProjectId == projectId && f.UserId == userId);

            return MapToViewModel(project, isFavorite);
        }

        public async Task<ProjectViewModel> CreateProjectAsync(Guid userId, CreateProjectRequestViewModel model)
        {
            // BR-07-01: EndDate must be on or after StartDate if both are set
            ValidateProjectDates(model.StartDate, model.EndDate);

            var project = new Project
            {
                Id = Guid.NewGuid(),
                WorkspaceId = model.WorkspaceId,
                Name = model.Name,
                Description = model.Description,
                Status = "Active",
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Budget = model.Budget,
                Client = model.Client,
                ProjectManagerUserId = userId,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };

            var creatorMember = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = userId,
                ProjectScopedRole = "Owner",
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.Projects.Add(project);
            _dbContext.ProjectMembers.Add(creatorMember);

            await _dbContext.SaveChangesAsync();

            project.Members.Add(creatorMember);
            return MapToViewModel(project, isFavorite: false);
        }

        public async Task<ProjectViewModel> UpdateProjectAsync(Guid userId, UpdateProjectRequestViewModel model)
        {
            var project = await _dbContext.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == model.ProjectId);

            if (project == null)
            {
                throw new DomainException("Project not found.");
            }

            ValidateProjectDates(model.StartDate, model.EndDate);

            project.Name = model.Name;
            project.Description = model.Description;
            project.Status = model.Status;
            project.StartDate = model.StartDate;
            project.EndDate = model.EndDate;
            project.Budget = model.Budget;
            project.Client = model.Client;

            await _dbContext.SaveChangesAsync();

            var isFavorite = await _dbContext.ProjectFavorites
                .AnyAsync(f => f.ProjectId == model.ProjectId && f.UserId == userId);

            return MapToViewModel(project, isFavorite);
        }

        public async Task<bool> ArchiveProjectAsync(Guid userId, Guid projectId)
        {
            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null)
                throw new DomainException("Project not found.");

            project.IsArchived = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnarchiveProjectAsync(Guid userId, Guid projectId)
        {
            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null)
                throw new DomainException("Project not found.");

            project.IsArchived = false;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid projectId)
        {
            var fav = await _dbContext.ProjectFavorites
                .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.UserId == userId);

            if (fav == null)
            {
                _dbContext.ProjectFavorites.Add(new ProjectFavorite
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _dbContext.ProjectFavorites.Remove(fav);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProjectMemberViewModel>> GetProjectMembersAsync(Guid projectId, Guid userId)
        {
            var members = await _dbContext.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .OrderByDescending(m => m.JoinedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(members.Select(m => m.UserId));

            return members.Select(m => new ProjectMemberViewModel
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                UserId = m.UserId,
                FullName = userLookup.TryGetValue(m.UserId, out var u) ? u.FullName : "Unknown User",
                Email = userLookup.TryGetValue(m.UserId, out var u2) ? (u2.Email ?? string.Empty) : string.Empty,
                Role = m.ProjectScopedRole,
                JoinedAt = m.JoinedAt
            }).ToList();
        }

        private async Task<Dictionary<Guid, UserLookup>> GetUserLookupAsync(IEnumerable<Guid> userIds)
        {
            var distinctIds = userIds.Distinct().ToList();
            return await _dbContext.UserLookups
                .Where(u => distinctIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        public async Task<ProjectMemberViewModel> AddMemberAsync(Guid userId, AddProjectMemberRequestViewModel model)
        {
            var isMember = await _dbContext.ProjectMembers
                .AnyAsync(m => m.ProjectId == model.ProjectId && m.UserId == model.UserId);

            if (isMember)
            {
                throw new DomainException("User is already a member of this project.");
            }

            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = model.ProjectId,
                UserId = model.UserId,
                ProjectScopedRole = string.IsNullOrEmpty(model.Role) ? "Developer" : model.Role,
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.ProjectMembers.Add(member);
            await _dbContext.SaveChangesAsync();

            var addedUser = await _dbContext.UserLookups.FirstOrDefaultAsync(u => u.Id == member.UserId);

            return new ProjectMemberViewModel
            {
                Id = member.Id,
                ProjectId = member.ProjectId,
                UserId = member.UserId,
                FullName = addedUser?.FullName ?? "Unknown User",
                Email = addedUser?.Email ?? string.Empty,
                Role = member.ProjectScopedRole,
                JoinedAt = member.JoinedAt
            };
        }

        public async Task<bool> RemoveMemberAsync(Guid userId, Guid projectId, Guid targetUserId)
        {
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == targetUserId);

            if (member != null)
            {
                _dbContext.ProjectMembers.Remove(member);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> UpdateMemberRoleAsync(Guid userId, Guid projectId, Guid targetUserId, string role)
        {
            var member = await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == targetUserId);

            if (member == null)
            {
                throw new DomainException("Project member not found.");
            }

            member.ProjectScopedRole = role;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ProjectJoinRequestViewModel> SubmitJoinRequestAsync(Guid userId, Guid projectId)
        {
            var isMember = await _dbContext.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);

            if (isMember)
            {
                throw new DomainException("You are already a member of this project.");
            }

            var existingRequest = await _dbContext.ProjectJoinRequests
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.RequestingUserId == userId && r.Status == "Pending");

            if (existingRequest != null)
            {
                throw new DomainException("You already have a pending join request for this project.");
            }

            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null)
                throw new DomainException("Project not found.");

            var request = new ProjectJoinRequest
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                RequestingUserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ProjectJoinRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            return new ProjectJoinRequestViewModel
            {
                RequestId = request.Id,
                ProjectId = projectId,
                ProjectName = project.Name,
                RequestingUserId = userId,
                RequestingUserName = "User",
                RequestingUserEmail = "user@taskplatform.com",
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };
        }

        public async Task<List<ProjectJoinRequestViewModel>> GetPendingJoinRequestsAsync(Guid projectId, Guid userId)
        {
            var requests = await _dbContext.ProjectJoinRequests
                .Include(r => r.Project)
                .Where(r => r.ProjectId == projectId && r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(r => new ProjectJoinRequestViewModel
            {
                RequestId = r.Id,
                ProjectId = r.ProjectId,
                ProjectName = r.Project?.Name ?? "",
                RequestingUserId = r.RequestingUserId,
                RequestingUserName = "Applicant",
                RequestingUserEmail = "applicant@taskplatform.com",
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> ResolveJoinRequestAsync(Guid userId, ResolveJoinRequestViewModel model)
        {
            var request = await _dbContext.ProjectJoinRequests
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);

            if (request == null || request.Status != "Pending")
            {
                throw new DomainException("Invalid or non-pending join request.");
            }

            request.Status = model.Approve ? "Approved" : "Rejected";
            request.ResolvedByUserId = userId;

            if (model.Approve)
            {
                // BR-08-01: Default role assigned is Developer (lowest role with create rights), not Owner/Admin
                var isMember = await _dbContext.ProjectMembers
                    .AnyAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.RequestingUserId);

                if (!isMember)
                {
                    _dbContext.ProjectMembers.Add(new ProjectMember
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = request.ProjectId,
                        UserId = request.RequestingUserId,
                        ProjectScopedRole = "Developer",
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static void ValidateProjectDates(DateTime? startDate, DateTime? endDate)
        {
            // BR-07-01: EndDate must be on or after StartDate if both are set
            if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            {
                throw new DomainException("Project End Date must be on or after Start Date.");
            }
        }

        private static ProjectViewModel MapToViewModel(Project p, bool isFavorite)
        {
            return new ProjectViewModel
            {
                Id = p.Id,
                WorkspaceId = p.WorkspaceId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Budget = p.Budget,
                Client = p.Client,
                ProjectManagerUserId = p.ProjectManagerUserId,
                IsArchived = p.IsArchived,
                IsFavorite = isFavorite,
                CreatedAt = p.CreatedAt,
                MemberCount = p.Members?.Count ?? 0
            };
        }
    }
}
