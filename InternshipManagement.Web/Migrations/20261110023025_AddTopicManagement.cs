
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    public partial class AddTopicManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "longtext", nullable: false),
                    objectives = table.Column<string>(type: "longtext", nullable: true),
                    specialty = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    technologies = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    requirements = table.Column<string>(type: "longtext", nullable: true),
                    topic_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    max_students = table.Column<int>(type: "int", nullable: true),
                    max_groups = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", maxLength: 255, nullable: false),
                    source_topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.id);

                    table.ForeignKey(
                        name: "FK_topics_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_topics_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_topics_topics_source_topic_id",
                        column: x => x.source_topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4")
                .Annotation("MySQL:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "topic_attachments",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_user_id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", maxLength: 255, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_attachments", x => x.id);

                    table.ForeignKey(
                        name: "FK_topic_attachments_AspNetUsers_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_topic_attachments_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4")
                .Annotation("MySQL:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "topic_lecturers",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    topic_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    lecturer_user_id = table.Column<string>(type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", maxLength: 255, nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topic_lecturers", x => x.id);

                    table.ForeignKey(
                        name: "FK_topic_lecturers_AspNetUsers_lecturer_user_id",
                        column: x => x.lecturer_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_topic_lecturers_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4")
                .Annotation("MySQL:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_topic_attachments_topic_id",
                table: "topic_attachments",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_topic_attachments_uploaded_by_user_id",
                table: "topic_attachments",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_topic_lecturers_lecturer_user_id",
                table: "topic_lecturers",
                column: "lecturer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_topic_lecturers_topic_id_is_primary",
                table: "topic_lecturers",
                columns: new[] { "topic_id", "is_primary" });

            migrationBuilder.CreateIndex(
                name: "IX_topic_lecturers_topic_id_lecturer_user_id",
                table: "topic_lecturers",
                columns: new[] { "topic_id", "lecturer_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_topics_campaign_id_status",
                table: "topics",
                columns: new[] { "campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_topics_created_by_user_id",
                table: "topics",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_topics_source_topic_id",
                table: "topics",
                column: "source_topic_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "topic_attachments");
            migrationBuilder.DropTable(name: "topic_lecturers");
            migrationBuilder.DropTable(name: "topics");
        }
    }
}
