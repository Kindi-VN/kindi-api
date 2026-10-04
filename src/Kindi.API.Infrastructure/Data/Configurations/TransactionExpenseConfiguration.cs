using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class TransactionExpenseConfiguration : IEntityTypeConfiguration<TransactionExpense>
{
    public void Configure(EntityTypeBuilder<TransactionExpense> builder)
    {
        builder.ToTable("TransactionExpenses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasOne(x => x.TransactionRevenue)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.TransactionRevenueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
