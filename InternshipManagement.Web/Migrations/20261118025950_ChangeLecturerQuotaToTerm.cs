
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace InternshipManagement.Web.Migrations
{
    public partial class ChangeLecturerQuotaToTerm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tạo bảng hạn mức theo học kỳ
            // Chưa tạo khóa ngoại tới AspNetUsers để xử lý collation trước.
            migrationBuilder.CreateTable(
                name: "lecturer_term_quotas",
                columns: table => new
                {
                    id = table.Column<ulong>(
                        type: "bigint unsigned",
                        nullable: false)
                        .Annotation(
                            "MySQL:ValueGenerationStrategy",
                            MySQLValueGenerationStrategy.IdentityColumn),

                    term_id = table.Column<ulong>(
                        type: "bigint unsigned",
                        nullable: false),

                    lecturer_user_id = table.Column<string>(
                        type: "varchar(255)",
                        maxLength: 255,
                        nullable: false),

                    capacity = table.Column<int>(
                        type: "int",
                        nullable: false),

                    updated_at = table.Column<DateTime>(
                        type: "datetime(6)",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_lecturer_term_quotas",
                        x => x.id);

                    table.ForeignKey(
                        name: "FK_lecturer_term_quotas_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 2. Đồng bộ charset và collation với AspNetUsers.Id
            migrationBuilder.Sql(@"
                ALTER TABLE lecturer_term_quotas
                MODIFY lecturer_user_id
                varchar(255)
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_unicode_ci
                NOT NULL;
            ");

            // 3. Tạo khóa ngoại tới AspNetUsers sau khi đồng bộ collation
            migrationBuilder.AddForeignKey(
                name: "FK_lecturer_term_quotas_AspNetUsers_lecturer_user_id",
                table: "lecturer_term_quotas",
                column: "lecturer_user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 4. Tạo index cho bảng hạn mức mới
            migrationBuilder.CreateIndex(
                name: "IX_lecturer_term_quotas_lecturer_user_id",
                table: "lecturer_term_quotas",
                column: "lecturer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_term_quotas_term_id_lecturer_user_id",
                table: "lecturer_term_quotas",
                columns: new[] { "term_id", "lecturer_user_id" },
                unique: true);

            // 5. Thêm major_id cho topics ở trạng thái nullable
            migrationBuilder.AddColumn<ulong>(
                name: "major_id",
                table: "topics",
                type: "bigint unsigned",
                nullable: true);

            // 6. Chuyển hạn mức cũ sang hạn mức theo học kỳ
            migrationBuilder.Sql(@"
                INSERT INTO lecturer_term_quotas
                    (term_id, lecturer_user_id, capacity, updated_at)
                SELECT
                    c.term_id,
                    q.lecturer_user_id,
                    SUM(q.capacity),
                    MAX(q.updated_at)
                FROM lecturer_campaign_quotas q
                INNER JOIN campaigns c ON c.id = q.campaign_id
                GROUP BY c.term_id, q.lecturer_user_id;
            ");

            // 7. Cập nhật chuyên ngành cho đề tài từ chiến dịch
            migrationBuilder.Sql(@"
                UPDATE topics t
                INNER JOIN campaigns c ON c.id = t.campaign_id
                SET t.major_id = c.major_id;
            ");

            // 8. Chuyển major_id thành NOT NULL
            migrationBuilder.AlterColumn<ulong>(
                name: "major_id",
                table: "topics",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);

            // 9. Tạo index cho topics.major_id
            migrationBuilder.CreateIndex(
                name: "IX_topics_major_id",
                table: "topics",
                column: "major_id");

            // 10. Tạo khóa ngoại topics.major_id
            migrationBuilder.AddForeignKey(
                name: "FK_topics_majors_major_id",
                table: "topics",
                column: "major_id",
                principalTable: "majors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 11. Chỉ xóa bảng cũ sau khi đã chuyển dữ liệu
            migrationBuilder.DropTable(
                name: "lecturer_campaign_quotas");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa khóa ngoại và index của topics.major_id
            migrationBuilder.DropForeignKey(
                name: "FK_topics_majors_major_id",
                table: "topics");

            migrationBuilder.DropIndex(
                name: "IX_topics_major_id",
                table: "topics");

            // 2. Xóa major_id khỏi topics
            migrationBuilder.DropColumn(
                name: "major_id",
                table: "topics");

            // 3. Xóa bảng hạn mức theo học kỳ
            migrationBuilder.DropTable(
                name: "lecturer_term_quotas");

            // 4. Tạo lại cấu trúc bảng hạn mức cũ
            // Lưu ý: không thể khôi phục chính xác dữ liệu từng chiến dịch
            // vì Up() đã cộng gộp hạn mức theo học kỳ.
            migrationBuilder.CreateTable(
                name: "lecturer_campaign_quotas",
                columns: table => new
                {
                    id = table.Column<ulong>(
                        type: "bigint unsigned",
                        nullable: false)
                        .Annotation(
                            "MySQL:ValueGenerationStrategy",
                            MySQLValueGenerationStrategy.IdentityColumn),

                    campaign_id = table.Column<ulong>(
                        type: "bigint unsigned",
                        nullable: false),

                    lecturer_user_id = table.Column<string>(
                        type: "varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
                        maxLength: 255,
                        nullable: false),

                    capacity = table.Column<int>(
                        type: "int",
                        nullable: false),

                    updated_at = table.Column<DateTime>(
                        type: "datetime(6)",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_lecturer_campaign_quotas",
                        x => x.id);

                    table.ForeignKey(
                        name: "FK_lecturer_campaign_quotas_AspNetUsers_lecturer_user_id",
                        column: x => x.lecturer_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_lecturer_campaign_quotas_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // 5. Tạo index cho bảng hạn mức cũ
            migrationBuilder.CreateIndex(
                name: "IX_lecturer_campaign_quotas_campaign_id_lecturer_user_id",
                table: "lecturer_campaign_quotas",
                columns: new[] { "campaign_id", "lecturer_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lecturer_campaign_quotas_lecturer_user_id",
                table: "lecturer_campaign_quotas",
                column: "lecturer_user_id");
        }
    }
}
