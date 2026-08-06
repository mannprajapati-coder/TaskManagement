using System;
using System.Collections.Generic;
using Modules.Projects.Domain.IServices;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Infrastructure.Context;
using Modules.Workspaces.Domain.IServices;
using Moq;
using TaskPlatform.Shared.ViewModels.Project;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace TaskPlatform.Tests
{
    // TasksService now needs IProjectsService/IWorkspaceService to authorize mutations
    // (assignee, project owner, or workspace owner). These stubs keep existing BR tests
    // focused on their own rule by defaulting the acting user to a permitted project owner.
    internal static class TasksServiceTestFactory
    {
        public static TasksService Create(TasksDbContext dbContext, bool grantPermission = true)
        {
            var projectsService = new Mock<IProjectsService>();
            var workspaceService = new Mock<IWorkspaceService>();

            projectsService
                .Setup(p => p.GetProjectByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Guid projectId, Guid userId) => new ProjectViewModel
                {
                    Id = projectId,
                    WorkspaceId = Guid.NewGuid()
                });

            projectsService
                .Setup(p => p.GetProjectMembersAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Guid projectId, Guid userId) => grantPermission
                    ? new List<ProjectMemberViewModel>
                    {
                        new ProjectMemberViewModel { ProjectId = projectId, UserId = userId, Role = "Owner" }
                    }
                    : new List<ProjectMemberViewModel>());

            workspaceService
                .Setup(w => w.GetWorkspaceByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Guid workspaceId, Guid userId) => new WorkspaceViewModel
                {
                    Id = workspaceId,
                    OwnerUserId = grantPermission ? userId : Guid.NewGuid()
                });

            return new TasksService(dbContext, projectsService.Object, workspaceService.Object);
        }
    }
}
