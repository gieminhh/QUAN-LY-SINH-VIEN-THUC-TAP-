using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class Issue32Applications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DoanhNghiepId",
                table: "AspNetUsers",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "internship_positions",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    campaign_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    internship_place_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "longtext", nullable: false),
                    requirements = table.Column<string>(type: "longtext", nullable: true),
                    benefits = table.Column<string>(type: "longtext", nullable: true),
                    skills = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    slots = table.Column<int>(type: "int", nullable: false),
                    deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    approval_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    review_note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_open = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_by_user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internship_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_internship_positions_AspNetUsers_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_internship_positions_AspNetUsers_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_internship_positions_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_internship_positions_internship_places_internship_place_id",
                        column: x => x.internship_place_id,
                        principalTable: "internship_places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ho_so_ung_tuyen",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ViTriThucTapId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    SinhVienId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    GhiChuPhanHoi = table.Column<string>(type: "longtext", nullable: true),
                    FileCV = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ho_so_ung_tuyen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ho_so_ung_tuyen_AspNetUsers_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ho_so_ung_tuyen_internship_positions_ViTriThucTapId",
                        column: x => x.ViTriThucTapId,
                        principalTable: "internship_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DoanhNghiepId",
                table: "AspNetUsers",
                column: "DoanhNghiepId");

            migrationBuilder.CreateIndex(
                name: "IX_ho_so_ung_tuyen_SinhVienId",
                table: "ho_so_ung_tuyen",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ho_so_ung_tuyen_ViTriThucTapId_SinhVienId",
                table: "ho_so_ung_tuyen",
                columns: new[] { "ViTriThucTapId", "SinhVienId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internship_positions_campaign_id_approval_status",
                table: "internship_positions",
                columns: new[] { "campaign_id", "approval_status" });

            migrationBuilder.CreateIndex(
                name: "IX_internship_positions_created_by_user_id",
                table: "internship_positions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_positions_internship_place_id",
                table: "internship_positions",
                column: "internship_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_internship_positions_reviewed_by_user_id",
                table: "internship_positions",
                column: "reviewed_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_DoanhNghieps_DoanhNghiepId",
                table: "AspNetUsers",
                column: "DoanhNghiepId",
                principalTable: "DoanhNghieps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_DoanhNghieps_DoanhNghiepId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ho_so_ung_tuyen");

            migrationBuilder.DropTable(
                name: "internship_positions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DoanhNghiepId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DoanhNghiepId",
                table: "AspNetUsers");
        }
    }
}
