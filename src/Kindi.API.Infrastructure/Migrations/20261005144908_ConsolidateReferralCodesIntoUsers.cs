using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <summary>
    /// Quy mã chia sẻ (refcode) về MỘT nguồn duy nhất là <c>Users.ReferralCode</c>:
    /// - chuyển mã chia sẻ đang nằm trên hồ sơ CTV sang tài khoản, GIỮ NGUYÊN giá trị
    ///   nên mọi link chia sẻ đã phát tán vẫn hoạt động;
    /// - bỏ cột mã chia sẻ ở <c>Collaborators</c> (mã hồ sơ CTV vẫn còn ở <c>CollaboratorCode</c>)
    ///   và ở <c>Partners</c> (người giới thiệu đối tác nay nằm ở <c>Users.ReferredByCode</c>);
    /// - thêm ràng buộc duy nhất cho mã chia sẻ và cập nhật mặc định tiền tố/độ dài trong Cài đặt chung.
    /// </summary>
    public partial class ConsolidateReferralCodesIntoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Chuyển mã chia sẻ của CTV sang tài khoản (chỉ điền khi tài khoản chưa có mã).
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""ReferralCode"" = c.""ReferralCode"",
                    ""UpdatedAt"" = NOW()
                FROM ""Collaborators"" c
                WHERE c.""UserId"" = u.""Id""
                  AND c.""ReferralCode"" IS NOT NULL
                  AND u.""ReferralCode"" IS NULL;
            ");

            // 2) Mã chia sẻ phải duy nhất giữa các tài khoản.
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_ReferralCode_Unique""
                ON ""Users"" (""ReferralCode"")
                WHERE ""ReferralCode"" IS NOT NULL;
            ");

            // 3) Cài đặt chung còn ở mặc định cũ (CTV- / 6) thì đổi sang mặc định mới (KND / 8).
            migrationBuilder.Sql(@"
                UPDATE ""SystemSettings""
                SET ""ReferralCodePrefix"" = 'KND',
                    ""ReferralCodeLength"" = 8
                WHERE ""ReferralCodePrefix"" = 'CTV-'
                  AND ""ReferralCodeLength"" = 6;
            ");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Collaborators");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Partners",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Collaborators",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            // Trả mã chia sẻ về hồ sơ CTV để bản cũ (đọc mã theo hồ sơ CTV) chạy lại được.
            migrationBuilder.Sql(@"
                UPDATE ""Collaborators"" c
                SET ""ReferralCode"" = u.""ReferralCode"",
                    ""UpdatedAt"" = NOW()
                FROM ""Users"" u
                WHERE u.""Id"" = c.""UserId""
                  AND u.""ReferralCode"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Collaborators""
                SET ""ReferralCode"" = ""CollaboratorCode""
                WHERE ""ReferralCode"" IS NULL;
            ");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_ReferralCode_Unique"";");

            migrationBuilder.Sql(@"
                UPDATE ""SystemSettings""
                SET ""ReferralCodePrefix"" = 'CTV-',
                    ""ReferralCodeLength"" = 6
                WHERE ""ReferralCodePrefix"" = 'KND'
                  AND ""ReferralCodeLength"" = 8;
            ");
        }
    }
}
