using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class CommissionConfigConfiguration : IEntityTypeConfiguration<CommissionConfig>
{
    public void Configure(EntityTypeBuilder<CommissionConfig> builder)
    {
        builder.ToTable("CommissionConfigs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Beneficiary).HasConversion<int>();
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Rate).HasPrecision(18, 2);
        builder.Property(x => x.MinOrderValue).HasPrecision(18, 2);
        builder.Property(x => x.MaxCommission).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(500);

        // Tra mức áp dụng cho một tài khoản: lọc theo bên nhận + tài khoản.
        builder.HasIndex(x => new { x.Beneficiary, x.UserId });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Tiers)
            .WithOne(x => x.CommissionConfig)
            .HasForeignKey(x => x.CommissionConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
