using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeAndUnaccent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bỏ dấu tiếng Việt ngay dưới DB cho truy vấn tìm kiếm (KindiDbFunctions.Unaccent).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            migrationBuilder.AddColumn<string>(
                name: "CompanyCode",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies",
                column: "CompanyCode",
                unique: true,
                filter: "\"CompanyCode\" IS NOT NULL");

            // Sinh mã cho công ty đã có trước migration này: đánh số thứ tự theo Id
            // (không cắt md5 — xem AddEntityCodes: mã trùng sẽ vi phạm unique index).
            migrationBuilder.Sql(@"UPDATE ""Companies"" AS t SET ""CompanyCode"" = 'CMP-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""Companies"" WHERE ""CompanyCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "Companies");
        }
    }
}
