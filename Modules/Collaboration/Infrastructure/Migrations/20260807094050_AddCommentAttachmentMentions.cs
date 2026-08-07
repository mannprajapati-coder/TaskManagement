using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collaboration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAttachmentMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MentionedAttachmentIdsJson",
                table: "TaskComments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MentionedAttachmentIdsJson",
                table: "TaskComments");
        }
    }
}
