using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Collaboration.Application.Services;
using Modules.Collaboration.Infrastructure.Context;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1701_AttachmentStorageTests
    {
        [Fact]
        public async Task AddTaskAttachmentAsync_ValidFile_ShouldStoreMetadata()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<CollaborationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new CollaborationDbContext(options);
            var service = new CollaborationService(dbContext);
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            // Act
            var attachment = await service.AddTaskAttachmentAsync(
                userId, taskId, "specs.pdf", "/storage/tasks/specs.pdf", 1024500, "application/pdf"
            );

            // Assert
            attachment.Should().NotBeNull();
            attachment.FileName.Should().Be("specs.pdf");
            attachment.FileSize.Should().Be(1024500);

            var list = await service.GetTaskAttachmentsAsync(taskId);
            list.Should().ContainSingle(a => a.FileName == "specs.pdf");
        }
    }
}
