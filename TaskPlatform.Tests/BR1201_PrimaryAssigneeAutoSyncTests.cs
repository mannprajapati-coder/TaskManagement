using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Task;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1201_PrimaryAssigneeAutoSyncTests
    {
        [Fact]
        public async Task CreateTaskAsync_WithPrimaryAssignee_ShouldAutoCreateTaskAssigneeRecord()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var creatorId = Guid.NewGuid();
            var assigneeUserId = Guid.NewGuid();

            var createModel = new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Assigned Task",
                PrimaryAssigneeUserId = assigneeUserId
            };

            // Act
            var createdResp = await service.CreateTaskAsync(creatorId, createModel);
            var createdTask = createdResp.Data!;

            // Assert
            createdTask.Should().NotBeNull();
            createdTask.PrimaryAssigneeUserId.Should().Be(assigneeUserId);

            var assignees = await dbContext.TaskAssignees
                .FirstOrDefaultAsync(a => a.TaskId == createdTask.Id && a.UserId == assigneeUserId);

            assignees.Should().NotBeNull();
        }
    }
}
