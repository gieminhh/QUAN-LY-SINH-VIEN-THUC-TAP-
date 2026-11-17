using System;
using InternshipManagement.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261114203140_AddTopicProposals")]
    public partial class AddTopicProposals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "topic_proposals",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    student_user_id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", maxLength: 255, nullable: false),
                    lecturer_user_id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "longtext", nullable: false),
                    objectives = table.Column<string>(type: "longtext", maxLength: 1000, nullable: true),
                    student_note = table.Column<string>(type: "longtext", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    review_feedback = table.Column<string>(type: "longtext", maxLength: 2000, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_topic_proposals_AspNetUsers_lecturer_user_id",
                        column: x => x.lecturer_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_topic_proposals_AspNetUsers_student_user_id",
                        column: x => x.student_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_topic_proposals_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4")
                .Annotation("MySQL:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_topic_proposals_campaign_id_status",
                table: "topic_proposals",
                columns: new[] { "campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_proposals_lecturer_user_id_status",
                table: "topic_proposals",
                columns: new[] { "lecturer_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_proposals_student_user_id_created_at",
                table: "topic_proposals",
                columns: new[] { "student_user_id", "created_at" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "topic_proposals");
        }
    }
}
