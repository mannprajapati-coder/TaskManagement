using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Task;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1203_TaskModificationAuthorizationTests
    {
        [Fact]
        public async Task UpdateTaskStatusAsync_ByUnrelatedUser_ShouldThrowPermissionDeniedException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var creatorId = Guid.NewGuid();
            var creatingService = TasksServiceTestFactory.Create(dbContext);

            var task = await creatingService.CreateTaskAsync(creatorId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Task Nobody Else Owns"
            });

            // A second user who is neither the assignee, project owner, nor workspace owner
            var outsiderService = TasksServiceTestFactory.Create(dbContext, grantPermission: false);
            var outsiderId = Guid.NewGuid();

            // Act & Assert
            Func<Task> act = async () => await outsiderService.UpdateTaskStatusAsync(outsiderId, new UpdateTaskStatusRequestViewModel
            {
                TaskId = task.Id,
                Status = "InProgress"
            });

            await act.Should().ThrowAsync<PermissionDeniedException>();
        }

        [Fact]
        public async Task UpdateTaskStatusAsync_ByAssignee_ShouldSucceedEvenWithoutOwnerRole()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var creatingService = TasksServiceTestFactory.Create(dbContext);

            var task = await creatingService.CreateTaskAsync(creatorId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Assigned Task",
                PrimaryAssigneeUserId = assigneeId
            });

            // The assignee has no project/workspace owner role, but is still authorized as an assignee
            var assigneeService = TasksServiceTestFactory.Create(dbContext, grantPermission: false);

            // Act
            var updated = await assigneeService.UpdateTaskStatusAsync(assigneeId, new UpdateTaskStatusRequestViewModel
            {
                TaskId = task.Id,
                Status = "InProgress"
            });

            // Assert
            updated.Status.Should().Be("InProgress");
        }
    }
}
