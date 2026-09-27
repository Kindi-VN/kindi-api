using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "PurchaseRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "OfferRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "GroupBuyingRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "GroupBuyingParticipants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Collaborators",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "BusinessGroupPosts",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithShareLink",
                table: "BusinessGroupPosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "BusinessGroupMembers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            // CTV đã có trước migration này dùng mã chia sẻ riêng bằng mã CTV trên hồ sơ.
            migrationBuilder.Sql("UPDATE \"Collaborators\" SET \"ReferralCode\" = \"CollaboratorCode\" WHERE \"ReferralCode\" IS NULL AND \"CollaboratorCode\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "OfferRequests");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "GroupBuyingRequests");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "GroupBuyingParticipants");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Collaborators");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "BusinessGroupPosts");

            migrationBuilder.DropColumn(
                name: "WithShareLink",
                table: "BusinessGroupPosts");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "BusinessGroupMembers");
        }
    }
}
