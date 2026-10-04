using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class TransactionRevenueConfiguration : IEntityTypeConfiguration<TransactionRevenue>
{
    public void Configure(EntityTypeBuilder<TransactionRevenue> builder)
    {
        builder.ToTable("TransactionRevenues");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ReferenceCode).HasMaxLength(50);
        builder.Property(x => x.GrossRevenue).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetRevenue).HasPrecision(18, 2);
        builder.Property(x => x.TotalCommission).HasPrecision(18, 2);
        builder.Property(x => x.ExtraCost).HasPrecision(18, 2);
        builder.Property(x => x.ActualRevenue).HasPrecision(18, 2);
        builder.Property(x => x.ExtraCostNote).HasMaxLength(500);
        builder.Property(x => x.ConfirmedBy).HasMaxLength(100);

        // Mỗi giao dịch chỉ có một bản khai đang dùng.
        builder.HasIndex(x => new { x.Type, x.ReferenceId }).IsUnique();

        builder.HasMany(x => x.Commissions)
            .WithOne(x => x.TransactionRevenue)
            .HasForeignKey(x => x.TransactionRevenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
