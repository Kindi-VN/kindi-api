using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameReferralCodeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReferredByCode",
                table: "Users",
                newName: "AccountReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferredAt",
                table: "Users",
                newName: "AccountReferrerAt");

            migrationBuilder.RenameIndex(
                name: "IX_Users_ReferredByCode",
                table: "Users",
                newName: "IX_Users_AccountReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "ReferralEvents",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameIndex(
                name: "IX_ReferralEvents_ReferralCode",
                table: "ReferralEvents",
                newName: "IX_ReferralEvents_RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "PurchaseRequests",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "OfferRequests",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "GroupBuyingRequests",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "GroupBuyingParticipants",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "BusinessGroupPosts",
                newName: "RecordReferrerCode");

            migrationBuilder.RenameColumn(
                name: "ReferralCode",
                table: "BusinessGroupMembers",
                newName: "RecordReferrerCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountReferrerCode",
                table: "Users",
                newName: "ReferredByCode");

            migrationBuilder.RenameColumn(
                name: "AccountReferrerAt",
                table: "Users",
                newName: "ReferredAt");

            migrationBuilder.RenameIndex(
                name: "IX_Users_AccountReferrerCode",
                table: "Users",
                newName: "IX_Users_ReferredByCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "ReferralEvents",
                newName: "ReferralCode");

            migrationBuilder.RenameIndex(
                name: "IX_ReferralEvents_RecordReferrerCode",
                table: "ReferralEvents",
                newName: "IX_ReferralEvents_ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "PurchaseRequests",
                newName: "ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "OfferRequests",
                newName: "ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "GroupBuyingRequests",
                newName: "ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "GroupBuyingParticipants",
                newName: "ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "BusinessGroupPosts",
                newName: "ReferralCode");

            migrationBuilder.RenameColumn(
                name: "RecordReferrerCode",
                table: "BusinessGroupMembers",
                newName: "ReferralCode");
        }
    }
}
