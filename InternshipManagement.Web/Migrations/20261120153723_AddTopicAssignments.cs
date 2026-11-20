using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "topic_assignment_groups",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    group_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_assignment_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_topic_assignment_groups_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "topic_proposals",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    student_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    lecturer_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "topic_student_assignments",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    student_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    group_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    adjustment_reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    adjusted_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    adjusted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                        name: "FK_topic_student_assignments_topic_assignment_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "topic_assignment_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_topic_student_assignments_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_topic_assignment_groups_topic_id",
                table: "topic_assignment_groups",
                column: "topic_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_topic_student_assignments_group_id",
                table: "topic_student_assignments",
                column: "group_id");

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
                name: "topic_proposals");

            migrationBuilder.DropTable(
                name: "topic_student_assignments");

            migrationBuilder.DropTable(
                name: "topic_assignment_groups");

        }
    }
}
