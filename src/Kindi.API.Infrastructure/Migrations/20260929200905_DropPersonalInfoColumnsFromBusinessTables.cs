using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropPersonalInfoColumnsFromBusinessTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thông tin cá nhân gom hết về bảng Users: thêm cột Zalo nếu chưa có, chuyển dữ liệu từ hồ sơ
            // do quản trị viên sửa (bảng CTV, đối tác) sang, rồi bỏ cột ở các bảng nghiệp vụ.
            // Mọi câu lệnh đều kiểm tra tồn tại trước khi làm nên chạy lại nhiều lần vẫn an toàn.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(20);
            ");

            // Chỉ chuyển dữ liệu khi các cột nghiệp vụ còn tồn tại (chạy lại sau khi đã bỏ cột thì bỏ qua).
            migrationBuilder.Sql(@"
                DO $do$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_schema = 'public' AND table_name = 'Collaborators' AND column_name = 'FullName') THEN
                        DROP TABLE IF EXISTS ""_profile_info"";
                        CREATE TEMP TABLE ""_profile_info"" ON COMMIT DROP AS
                        SELECT DISTINCT ON (""UserId"") ""UserId"", ""FullName"", ""Phone"", ""Email"", ""Zalo""
                        FROM (
                            SELECT ""UserId"", ""FullName"", ""Phone"", ""Email"", ""Zalo"", 1 AS ""Pri"", COALESCE(""UpdatedAt"", ""CreatedAt"") AS ""Stamp"" FROM ""Collaborators"" WHERE ""UserId"" IS NOT NULL
                            UNION ALL
                            SELECT ""UserId"", ""FullName"", ""Phone"", ""Email"", NULL, 2, COALESCE(""UpdatedAt"", ""CreatedAt"") FROM ""Partners"" WHERE ""UserId"" IS NOT NULL
                        ) AS src
                        ORDER BY ""UserId"", ""Pri"", ""Stamp"" DESC NULLS LAST;

                        UPDATE ""Users"" u SET ""FullName"" = b.""FullName"", ""UpdatedAt"" = NOW()
                        FROM ""_profile_info"" b
                        WHERE u.""Id"" = b.""UserId"" AND COALESCE(b.""FullName"", '') <> '' AND b.""FullName"" <> u.""FullName"";

                        UPDATE ""Users"" u SET ""Zalo"" = b.""Zalo"", ""UpdatedAt"" = NOW()
                        FROM ""_profile_info"" b
                        WHERE u.""Id"" = b.""UserId"" AND COALESCE(u.""Zalo"", '') = '' AND COALESCE(b.""Zalo"", '') <> '';

                        UPDATE ""Users"" u SET ""Phone"" = b.""Phone"", ""UpdatedAt"" = NOW()
                        FROM ""_profile_info"" b
                        WHERE u.""Id"" = b.""UserId"" AND COALESCE(u.""Phone"", '') = '' AND COALESCE(b.""Phone"", '') <> '';

                        UPDATE ""Users"" u SET ""Email"" = b.""Email"", ""UpdatedAt"" = NOW()
                        FROM ""_profile_info"" b
                        WHERE u.""Id"" = b.""UserId"" AND COALESCE(u.""Email"", '') = '' AND COALESCE(b.""Email"", '') <> '';

                        DROP TABLE IF EXISTS ""_profile_info"";
                    END IF;
                END
                $do$;
            ");

            // Bỏ cột và index ở các bảng nghiệp vụ (bảng nào không còn thì bỏ qua).
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_Partners_Email"";
                DROP INDEX IF EXISTS ""IX_Partners_Phone"";
                DROP INDEX IF EXISTS ""IX_Collaborators_Email"";
                DROP INDEX IF EXISTS ""IX_Collaborators_Phone"";
                ALTER TABLE ""PurchaseRequests"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""Partners"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"";
                ALTER TABLE ""OfferRequests"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""GroupBuyingRequests"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""GroupBuyingParticipants"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""Collaborators"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""BusinessGroupMembers"" DROP COLUMN IF EXISTS ""Email"", DROP COLUMN IF EXISTS ""FullName"", DROP COLUMN IF EXISTS ""Phone"", DROP COLUMN IF EXISTS ""Zalo"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục cấu trúc cột/index ở các bảng nghiệp vụ và bỏ cột Zalo của Users (dữ liệu cá nhân
            // đã gom về Users nên các cột khôi phục trở về rỗng). Đều có kiểm tra tồn tại trước khi làm.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""Zalo"";
                ALTER TABLE ""PurchaseRequests"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100), ADD COLUMN IF NOT EXISTS ""FullName"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(20) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(20);
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""FullName"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(15) NOT NULL DEFAULT '';
                ALTER TABLE ""OfferRequests"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100), ADD COLUMN IF NOT EXISTS ""FullName"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(15) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(15);
                ALTER TABLE ""GroupBuyingRequests"" ADD COLUMN IF NOT EXISTS ""Email"" text NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""FullName"" text NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" text NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" text;
                ALTER TABLE ""GroupBuyingParticipants"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100), ADD COLUMN IF NOT EXISTS ""FullName"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(15) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(15);
                ALTER TABLE ""Collaborators"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100), ADD COLUMN IF NOT EXISTS ""FullName"" character varying(200) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(20) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(20);
                ALTER TABLE ""BusinessGroupMembers"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(100), ADD COLUMN IF NOT EXISTS ""FullName"" character varying(100) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Phone"" character varying(15) NOT NULL DEFAULT '', ADD COLUMN IF NOT EXISTS ""Zalo"" character varying(15);

                CREATE INDEX IF NOT EXISTS ""IX_Partners_Email"" ON ""Partners"" (""Email"");
                CREATE INDEX IF NOT EXISTS ""IX_Partners_Phone"" ON ""Partners"" (""Phone"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Collaborators_Email"" ON ""Collaborators"" (""Email"") WHERE ""Email"" IS NOT NULL AND ""Email"" <> '';
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Collaborators_Phone"" ON ""Collaborators"" (""Phone"") WHERE ""Phone"" <> '';
            ");
        }
    }
}
