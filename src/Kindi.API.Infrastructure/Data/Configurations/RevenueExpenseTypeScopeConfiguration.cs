using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class RevenueExpenseTypeScopeConfiguration : IEntityTypeConfiguration<RevenueExpenseTypeScope>
{
    public void Configure(EntityTypeBuilder<RevenueExpenseTypeScope> builder)
    {
        builder.ToTable("RevenueExpenseTypeScopes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType).HasConversion<int>();

        // Mỗi loại chi phí chỉ gắn một lần cho mỗi loại giao dịch.
        builder.HasIndex(x => new { x.RevenueExpenseTypeId, x.TransactionType }).IsUnique();
    }
}
