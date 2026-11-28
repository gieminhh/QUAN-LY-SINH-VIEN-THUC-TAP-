using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipRegistrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "internship_places",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    contact_person = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true),
                    contact_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    contact_email = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true),
                    default_position = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internship_places", x => x.id);
                    table.ForeignKey(
                        name: "FK_internship_places_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "internship_registrations",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    student_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    internship_place_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    position_title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    start_date = table.Column<DateTime>(type: "date", nullable: true),
                    end_date = table.Column<DateTime>(type: "date", nullable: true),
                    note = table.Column<string>(type: "longtext", nullable: true),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    stored_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    content_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    approval_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    rejection_reason = table.Column<string>(type: "longtext", nullable: true),
                    reviewed_by_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internship_registrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_internship_registrations_AspNetUsers_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_internship_registrations_AspNetUsers_student_user_id",
                        column: x => x.student_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_internship_registrations_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_internship_registrations_internship_places_internship_place_~",
                        column: x => x.internship_place_id,
                        principalTable: "internship_places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "internship_registration_reviews",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    registration_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    attempt_no = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    comment = table.Column<string>(type: "longtext", nullable: true),
                    place_name_snapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    position_snapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    actor_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internship_registration_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_internship_registration_reviews_AspNetUsers_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_internship_registration_reviews_internship_registrations_reg~",
                        column: x => x.registration_id,
                        principalTable: "internship_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_internship_places_created_by_user_id",
                table: "internship_places",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_places_is_active",
                table: "internship_places",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_internship_places_name",
                table: "internship_places",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_internship_registration_reviews_actor_user_id",
                table: "internship_registration_reviews",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_registration_reviews_registration_id_created_at",
                table: "internship_registration_reviews",
                columns: new[] { "registration_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_internship_registrations_campaign_id_approval_status",
                table: "internship_registrations",
                columns: new[] { "campaign_id", "approval_status" });

            migrationBuilder.CreateIndex(
                name: "IX_internship_registrations_campaign_id_student_user_id",
                table: "internship_registrations",
                columns: new[] { "campaign_id", "student_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_internship_registrations_internship_place_id",
                table: "internship_registrations",
                column: "internship_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_registrations_reviewed_by_user_id",
                table: "internship_registrations",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_registrations_student_user_id",
                table: "internship_registrations",
                column: "student_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "internship_registration_reviews");

            migrationBuilder.DropTable(
                name: "internship_registrations");

            migrationBuilder.DropTable(
                name: "internship_places");
        }
    }
}
