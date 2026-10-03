using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralEventCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ReferralCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferredUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    RefEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefEntityCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsGuestAccount = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralEvents_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferralEvents_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_CreatedAt",
                table: "ReferralEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_EventType",
                table: "ReferralEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_ReferralCode",
                table: "ReferralEvents",
                column: "ReferralCode");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_ReferralEventCode",
                table: "ReferralEvents",
                column: "ReferralEventCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_ReferredUserId",
                table: "ReferralEvents",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralEvents_ReferrerUserId",
                table: "ReferralEvents",
                column: "ReferrerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralEvents");
        }
    }
}
