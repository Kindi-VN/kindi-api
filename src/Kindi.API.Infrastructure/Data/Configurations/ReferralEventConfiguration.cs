using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class ReferralEventConfiguration : IEntityTypeConfiguration<ReferralEvent>
{
    public void Configure(EntityTypeBuilder<ReferralEvent> builder)
    {
        builder.ToTable("ReferralEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferralEventCode).HasMaxLength(30);
        builder.Property(x => x.ReferralCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RefEntityCode).HasMaxLength(30);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionRate).HasPrecision(5, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);

        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        // Mã sự kiện sinh tự động — Postgres cho phép nhiều NULL trên unique index nên không cần filter.
        builder.HasIndex(x => x.ReferralEventCode).IsUnique();

        // Tra cứu thống kê theo mã / theo người được giới thiệu / theo thời gian.
        builder.HasIndex(x => x.ReferralCode);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.ReferredUserId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Referrer)
            .WithMany()
            .HasForeignKey(x => x.ReferrerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Referred)
            .WithMany()
            .HasForeignKey(x => x.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
