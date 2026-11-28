using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCooperationToInternshipPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cooperation_note",
                table: "internship_places",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cooperation_status",
                table: "internship_places",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tax_code",
                table: "internship_places",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "internship_places",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cooperation_note",
                table: "internship_places");

            migrationBuilder.DropColumn(
                name: "cooperation_status",
                table: "internship_places");

            migrationBuilder.DropColumn(
                name: "tax_code",
                table: "internship_places");

            migrationBuilder.DropColumn(
                name: "website",
                table: "internship_places");
        }
    }
}
