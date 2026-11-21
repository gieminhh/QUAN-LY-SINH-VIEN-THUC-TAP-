using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNoPreferencePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "no_preference_policy",
                table: "campaigns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "random_seed",
                table: "campaigns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "student_topic_preferences",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_topic_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_topic_preferences_AspNetUsers_student_id",
                        column: x => x.student_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_topic_preferences_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "topic_student_assignments",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    student_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_student_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_topic_student_assignments_AspNetUsers_student_user_id",
                        column: x => x.student_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_topic_student_assignments_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_topic_student_assignments_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_student_topic_preferences_student_id_priority",
                table: "student_topic_preferences",
                columns: new[] { "student_id", "priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_topic_preferences_student_id_topic_id",
                table: "student_topic_preferences",
                columns: new[] { "student_id", "topic_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_topic_preferences_topic_id",
                table: "student_topic_preferences",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_topic_student_assignments_campaign_id_status",
                table: "topic_student_assignments",
                columns: new[] { "campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_student_assignments_student_user_id",
                table: "topic_student_assignments",
                column: "student_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_topic_student_assignments_topic_id_student_user_id",
                table: "topic_student_assignments",
                columns: new[] { "topic_id", "student_user_id" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_topic_preferences");

            migrationBuilder.DropTable(
                name: "topic_student_assignments");

            migrationBuilder.DropColumn(
                name: "no_preference_policy",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "random_seed",
                table: "campaigns");

        }
    }
}
