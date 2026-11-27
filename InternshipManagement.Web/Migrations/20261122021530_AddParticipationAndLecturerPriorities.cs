using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipationAndLecturerPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaign_participation_policies",
                columns: table => new
                {
                    CampaignId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    GroupsAllowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: false),
                    RegistrationOpensAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RegistrationClosesAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LecturerRankingEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RankingOpensAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RankingClosesAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_participation_policies", x => x.CampaignId);
                    table.CheckConstraint("chk_participation_group_size", "`MaxGroupSize` BETWEEN 2 AND 20");
                    table.ForeignKey(
                        name: "FK_campaign_participation_policies_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lecturer_priority_lists",
                columns: table => new
                {
                    TopicId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LecturerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Version = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lecturer_priority_lists", x => new { x.TopicId, x.LecturerId });
                    table.ForeignKey(
                        name: "FK_lecturer_priority_lists_AspNetUsers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lecturer_priority_lists_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "project_registration_groups",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LeaderId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_registration_groups", x => x.Id);
                    table.UniqueConstraint("AK_project_registration_groups_CampaignId_Id", x => new { x.CampaignId, x.Id });
                    table.ForeignKey(
                        name: "FK_project_registration_groups_AspNetUsers_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_registration_groups_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lecturer_student_priorities",
                columns: table => new
                {
                    TopicId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LecturerId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lecturer_student_priorities", x => new { x.TopicId, x.LecturerId, x.StudentId });
                    table.CheckConstraint("chk_lecturer_priority_rank", "`Rank` > 0");
                    table.ForeignKey(
                        name: "FK_lecturer_student_priorities_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lecturer_student_priorities_lecturer_priority_lists_TopicId_~",
                        columns: x => new { x.TopicId, x.LecturerId },
                        principalTable: "lecturer_priority_lists",
                        principalColumns: new[] { "TopicId", "LecturerId" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "campaign_participants",
                columns: table => new
                {
                    CampaignId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    GroupId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_participants", x => new { x.CampaignId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_campaign_participants_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_participants_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_participants_project_registration_groups_CampaignId~",
                        columns: x => new { x.CampaignId, x.GroupId },
                        principalTable: "project_registration_groups",
                        principalColumns: new[] { "CampaignId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "project_group_invitations",
                columns: table => new
                {
                    GroupId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_group_invitations", x => new { x.GroupId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_project_group_invitations_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_group_invitations_project_registration_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "project_registration_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_participants_CampaignId_GroupId",
                table: "campaign_participants",
                columns: new[] { "CampaignId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_campaign_participants_StudentId",
                table: "campaign_participants",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_priority_lists_LecturerId",
                table: "lecturer_priority_lists",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_student_priorities_StudentId",
                table: "lecturer_student_priorities",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_student_priorities_TopicId_LecturerId_Rank",
                table: "lecturer_student_priorities",
                columns: new[] { "TopicId", "LecturerId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_group_invitations_StudentId",
                table: "project_group_invitations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_project_registration_groups_LeaderId",
                table: "project_registration_groups",
                column: "LeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_participants");

            migrationBuilder.DropTable(
                name: "campaign_participation_policies");

            migrationBuilder.DropTable(
                name: "lecturer_student_priorities");

            migrationBuilder.DropTable(
                name: "project_group_invitations");

            migrationBuilder.DropTable(
                name: "lecturer_priority_lists");

            migrationBuilder.DropTable(
                name: "project_registration_groups");
        }
    }
}
