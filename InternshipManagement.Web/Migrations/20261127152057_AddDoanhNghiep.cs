using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDoanhNghiep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoanhNghieps",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Ten = table.Column<string>(type: "longtext", nullable: false),
                    MaSoThue = table.Column<string>(type: "varchar(255)", nullable: false),
                    LinhVuc = table.Column<string>(type: "longtext", nullable: true),
                    DiaChi = table.Column<string>(type: "longtext", nullable: true),
                    NguoiLienHe = table.Column<string>(type: "longtext", nullable: true),
                    SoDienThoai = table.Column<string>(type: "longtext", nullable: true),
                    Email = table.Column<string>(type: "longtext", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    FileHopTac = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoanhNghieps", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DoanhNghieps_MaSoThue",
                table: "DoanhNghieps",
                column: "MaSoThue",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoanhNghieps");
        }
    }
}
