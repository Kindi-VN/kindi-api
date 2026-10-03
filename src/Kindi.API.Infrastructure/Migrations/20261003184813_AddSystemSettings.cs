using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kindi.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupportEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupportPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FacebookUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    YoutubeUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ZaloUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CopyrightText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DefaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllowRegistration = table.Column<bool>(type: "boolean", nullable: false),
                    RequireEmailVerification = table.Column<bool>(type: "boolean", nullable: false),
                    MinPasswordLength = table.Column<int>(type: "integer", nullable: false),
                    AccessTokenMinutes = table.Column<int>(type: "integer", nullable: false),
                    RefreshTokenDays = table.Column<int>(type: "integer", nullable: false),
                    MaxFailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutMinutes = table.Column<int>(type: "integer", nullable: false),
                    ReferralCodePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReferralCodeLength = table.Column<int>(type: "integer", nullable: false),
                    CommissionAttributionDays = table.Column<int>(type: "integer", nullable: false),
                    MaxUploadSizeMb = table.Column<int>(type: "integer", nullable: false),
                    AllowedImageExtensions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AllowedDocumentExtensions = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MaxImagesPerPost = table.Column<int>(type: "integer", nullable: false),
                    RequirePostApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequireGroupApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    EnableEmailNotification = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotificationReplyTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
