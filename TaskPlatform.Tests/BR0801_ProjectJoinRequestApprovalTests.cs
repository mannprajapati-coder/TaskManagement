using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Projects.Application.Services;
using Modules.Projects.Domain.Entities;
using Modules.Projects.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Project;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0801_ProjectJoinRequestApprovalTests
    {
        [Fact]
        public async Task ResolveJoinRequestAsync_Approved_ShouldCreateMemberWithDeveloperRole()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ProjectsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ProjectsDbContext(options);
            var project = new Project { Id = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), Name = "Approval WS" };
            dbContext.Projects.Add(project);

            var requestingUserId = Guid.NewGuid();
            var joinRequest = new ProjectJoinRequest
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                RequestingUserId = requestingUserId,
                Status = "Pending"
            };
            dbContext.ProjectJoinRequests.Add(joinRequest);
            await dbContext.SaveChangesAsync();

            var service = new ProjectsService(dbContext);
            var managerUserId = Guid.NewGuid();

            // Act
            var resolveModel = new ResolveJoinRequestViewModel
            {
                RequestId = joinRequest.Id,
                Approve = true
            };
            var result = await service.ResolveJoinRequestAsync(managerUserId, resolveModel);

            // Assert
            result.Should().BeTrue();

            var newMember = await dbContext.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == requestingUserId);

            newMember.Should().NotBeNull();
            newMember!.ProjectScopedRole.Should().Be("Developer"); // BR-08-01: Default role assigned is Developer
        }
    }
}
