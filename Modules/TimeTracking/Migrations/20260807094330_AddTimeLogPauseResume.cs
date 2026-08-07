using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracking.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeLogPauseResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccumulatedSeconds",
                table: "TimeLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastResumedAt",
                table: "TimeLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TimeLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedSeconds",
                table: "TimeLogs");

            migrationBuilder.DropColumn(
                name: "LastResumedAt",
                table: "TimeLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TimeLogs");
        }
    }
}
