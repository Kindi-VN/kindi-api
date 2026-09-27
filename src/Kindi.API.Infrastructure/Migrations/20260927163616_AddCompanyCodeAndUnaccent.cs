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

            // Sinh mã cho các công ty đã có trước migration này.
            migrationBuilder.Sql("UPDATE \"Companies\" SET \"CompanyCode\" = 'CMP-' || upper(substr(md5(\"Id\"::text), 1, 6)) WHERE \"CompanyCode\" IS NULL;");
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
