using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matching_rounds",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OpenedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PublishedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matching_rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matching_rounds_AspNetUsers_OpenedBy",
                        column: x => x.OpenedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_rounds_AspNetUsers_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_rounds_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_published_results",
                columns: table => new
                {
                    RoundId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    TopicId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    TopicTitle = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matching_published_results", x => new { x.RoundId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_matching_published_results_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_published_results_matching_rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "matching_rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_published_results_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "matching_round_preferences",
                columns: table => new
                {
                    RoundId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StudentId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    TopicId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matching_round_preferences", x => new { x.RoundId, x.StudentId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_matching_round_preferences_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_round_preferences_matching_rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "matching_rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matching_round_preferences_topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_matching_published_results_StudentId",
                table: "matching_published_results",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_published_results_TopicId",
                table: "matching_published_results",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_round_preferences_RoundId_StudentId_Priority",
                table: "matching_round_preferences",
                columns: new[] { "RoundId", "StudentId", "Priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matching_round_preferences_StudentId",
                table: "matching_round_preferences",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_round_preferences_TopicId",
                table: "matching_round_preferences",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_matching_rounds_CampaignId_Number",
                table: "matching_rounds",
                columns: new[] { "CampaignId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matching_rounds_OpenedBy",
                table: "matching_rounds",
                column: "OpenedBy");

            migrationBuilder.CreateIndex(
                name: "IX_matching_rounds_PublishedBy",
                table: "matching_rounds",
                column: "PublishedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matching_published_results");

            migrationBuilder.DropTable(
                name: "matching_round_preferences");

            migrationBuilder.DropTable(
                name: "matching_rounds");
        }
    }
}
