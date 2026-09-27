using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialShareCode",
                table: "SocialShare",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialLikeCode",
                table: "SocialLike",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostTagCode",
                table: "PostTags",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupBuyingParticipantCode",
                table: "GroupBuyingParticipants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessGroupMemberCode",
                table: "BusinessGroupMembers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessGroupCommentCode",
                table: "BusinessGroupComments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialShare_SocialShareCode",
                table: "SocialShare",
                column: "SocialShareCode",
                unique: true,
                filter: "\"SocialShareCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLike_SocialLikeCode",
                table: "SocialLike",
                column: "SocialLikeCode",
                unique: true,
                filter: "\"SocialLikeCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_PostTagCode",
                table: "PostTags",
                column: "PostTagCode",
                unique: true,
                filter: "\"PostTagCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBuyingParticipants_GroupBuyingParticipantCode",
                table: "GroupBuyingParticipants",
                column: "GroupBuyingParticipantCode",
                unique: true,
                filter: "\"GroupBuyingParticipantCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGroupMembers_BusinessGroupMemberCode",
                table: "BusinessGroupMembers",
                column: "BusinessGroupMemberCode",
                unique: true,
                filter: "\"BusinessGroupMemberCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGroupComments_BusinessGroupCommentCode",
                table: "BusinessGroupComments",
                column: "BusinessGroupCommentCode",
                unique: true,
                filter: "\"BusinessGroupCommentCode\" IS NOT NULL");

            // Sinh mã cho bản ghi đã có trước migration này: đánh số thứ tự theo Id.
            // Không cắt md5 như bản trước: 6 ký tự md5 chỉ có 16^6 giá trị nên bảng nhiều
            // dòng sẽ sinh mã trùng, vi phạm unique index và app không khởi động được.
            migrationBuilder.Sql(@"UPDATE ""SocialShare"" AS t SET ""SocialShareCode"" = 'SHR-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""SocialShare"" WHERE ""SocialShareCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""SocialLike"" AS t SET ""SocialLikeCode"" = 'SLK-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""SocialLike"" WHERE ""SocialLikeCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""PostTags"" AS t SET ""PostTagCode"" = 'PTG-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""PostTags"" WHERE ""PostTagCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""GroupBuyingParticipants"" AS t SET ""GroupBuyingParticipantCode"" = 'GBPA-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""GroupBuyingParticipants"" WHERE ""GroupBuyingParticipantCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""BusinessGroupMembers"" AS t SET ""BusinessGroupMemberCode"" = 'BGM-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""BusinessGroupMembers"" WHERE ""BusinessGroupMemberCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
            migrationBuilder.Sql(@"UPDATE ""BusinessGroupComments"" AS t SET ""BusinessGroupCommentCode"" = 'GBC-' || lpad(s.rn::text, greatest(6, length(s.rn::text)), '0') FROM (SELECT ""Id"", row_number() OVER (ORDER BY ""Id"") AS rn FROM ""BusinessGroupComments"" WHERE ""BusinessGroupCommentCode"" IS NULL) AS s WHERE t.""Id"" = s.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SocialShare_SocialShareCode",
                table: "SocialShare");

            migrationBuilder.DropIndex(
                name: "IX_SocialLike_SocialLikeCode",
                table: "SocialLike");

            migrationBuilder.DropIndex(
                name: "IX_PostTags_PostTagCode",
                table: "PostTags");

            migrationBuilder.DropIndex(
                name: "IX_GroupBuyingParticipants_GroupBuyingParticipantCode",
                table: "GroupBuyingParticipants");

            migrationBuilder.DropIndex(
                name: "IX_BusinessGroupMembers_BusinessGroupMemberCode",
                table: "BusinessGroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_BusinessGroupComments_BusinessGroupCommentCode",
                table: "BusinessGroupComments");

            migrationBuilder.DropColumn(
                name: "SocialShareCode",
                table: "SocialShare");

            migrationBuilder.DropColumn(
                name: "SocialLikeCode",
                table: "SocialLike");

            migrationBuilder.DropColumn(
                name: "PostTagCode",
                table: "PostTags");

            migrationBuilder.DropColumn(
                name: "GroupBuyingParticipantCode",
                table: "GroupBuyingParticipants");

            migrationBuilder.DropColumn(
                name: "BusinessGroupMemberCode",
                table: "BusinessGroupMembers");

            migrationBuilder.DropColumn(
                name: "BusinessGroupCommentCode",
                table: "BusinessGroupComments");
        }
    }
}
