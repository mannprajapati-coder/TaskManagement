using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Domain.Entities;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Task;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR2101_CalendarEventFormattingTests
    {
        [Fact]
        public async Task GetCalendarEventsAsync_TasksWithDueDates_ShouldReturnFormattedEventsWithPriorityColors()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var tasksService = TasksServiceTestFactory.Create(dbContext);
            var calendarService = new CalendarService(dbContext);
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // CalendarService scopes tasks to the workspace via ProjectLookups (the read-only
            // projection of the Projects table) — seed that relationship for the test's project.
            dbContext.ProjectLookups.Add(new ProjectLookup { Id = projectId, WorkspaceId = workspaceId });
            await dbContext.SaveChangesAsync();

            var task = await tasksService.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = projectId,
                Title = "Critical System Release",
                Priority = "Urgent",
                DueDate = DateTime.UtcNow.AddDays(2)
            });

            // Act
            var events = await calendarService.GetCalendarEventsAsync(workspaceId, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(10));

            // Assert
            events.Should().ContainSingle(e => e.Id == task.Id.ToString());
            var calendarEvent = events[0];
            calendarEvent.Title.Should().Be("Critical System Release");
            calendarEvent.Color.Should().Be("#ef4444"); // Urgent red color
        }
    }
}
