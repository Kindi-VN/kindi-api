using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SystemName).HasMaxLength(200);
        builder.Property(x => x.SupportEmail).HasMaxLength(200);
        builder.Property(x => x.SupportPhone).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.WorkingHours).HasMaxLength(200);
        builder.Property(x => x.FacebookUrl).HasMaxLength(300);
        builder.Property(x => x.YoutubeUrl).HasMaxLength(300);
        builder.Property(x => x.ZaloUrl).HasMaxLength(300);
        builder.Property(x => x.TiktokUrl).HasMaxLength(300);
        builder.Property(x => x.InstagramUrl).HasMaxLength(300);
        builder.Property(x => x.XUrl).HasMaxLength(300);
        builder.Property(x => x.ThreadsUrl).HasMaxLength(300);
        builder.Property(x => x.LinkedinUrl).HasMaxLength(300);
        builder.Property(x => x.CopyrightText).HasMaxLength(300);
        builder.Property(x => x.PrivacyPolicy).HasColumnType("nvarchar(max)");
        builder.Property(x => x.TermsOfService).HasColumnType("nvarchar(max)");
        builder.Property(x => x.DefaultLanguage).HasMaxLength(10);
        builder.Property(x => x.TimeZone).HasMaxLength(100);
        builder.Property(x => x.CurrencySymbol).HasMaxLength(10);
        builder.Property(x => x.DateFormat).HasMaxLength(20);
        builder.Property(x => x.ReferralCodePrefix).HasMaxLength(20);
        builder.Property(x => x.AllowedImageExtensions).HasMaxLength(300);
        builder.Property(x => x.AllowedDocumentExtensions).HasMaxLength(300);
        builder.Property(x => x.NotificationSenderName).HasMaxLength(200);
        builder.Property(x => x.NotificationReplyTo).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.RevenueTaxPercent).HasPrecision(5, 2);
    }
}
