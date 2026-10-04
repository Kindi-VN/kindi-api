using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class TransactionCommissionConfiguration : IEntityTypeConfiguration<TransactionCommission>
{
    public void Configure(EntityTypeBuilder<TransactionCommission> builder)
    {
        builder.ToTable("TransactionCommissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Beneficiary).HasConversion<int>();
        builder.Property(x => x.Rate).HasPrecision(5, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        // Tra hoa hồng theo bên nhận của một giao dịch.
        builder.HasIndex(x => new { x.TransactionRevenueId, x.Beneficiary });
    }
}
