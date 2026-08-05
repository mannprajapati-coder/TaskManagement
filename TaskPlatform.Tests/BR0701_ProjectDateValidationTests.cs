using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Projects.Application.Services;
using Modules.Projects.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Project;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0701_ProjectDateValidationTests
    {
        [Fact]
        public async Task CreateProjectAsync_EndDateBeforeStartDate_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ProjectsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ProjectsDbContext(options);
            var service = new ProjectsService(dbContext);
            var userId = Guid.NewGuid();

            var model = new CreateProjectRequestViewModel
            {
                WorkspaceId = Guid.NewGuid(),
                Name = "Invalid Date Project",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(2) // End date before start date!
            };

            // Act & Assert
            Func<Task> act = async () => await service.CreateProjectAsync(userId, model);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*End Date must be on or after Start Date*");
        }

        [Fact]
        public async Task CreateProjectAsync_ValidDates_ShouldSucceed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ProjectsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new ProjectsDbContext(options);
            var service = new ProjectsService(dbContext);
            var userId = Guid.NewGuid();

            var model = new CreateProjectRequestViewModel
            {
                WorkspaceId = Guid.NewGuid(),
                Name = "Valid Date Project",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await service.CreateProjectAsync(userId, model);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Valid Date Project");
        }
    }
}
