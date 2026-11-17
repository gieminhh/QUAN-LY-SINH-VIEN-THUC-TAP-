using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approval_status",
                table: "topics",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING");
                migrationBuilder.Sql(
                "UPDATE topics SET approval_status = 'APPROVED';");

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "topics",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "topics",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by_user_id",
                table: "topics",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "topics");
        }
    }
}
