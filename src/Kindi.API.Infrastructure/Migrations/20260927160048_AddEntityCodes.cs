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
