using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class RevenueExpenseTypeConfiguration : IEntityTypeConfiguration<RevenueExpenseType>
{
    public void Configure(EntityTypeBuilder<RevenueExpenseType> builder)
    {
        builder.ToTable("RevenueExpenseTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);

        builder.HasMany(x => x.Scopes)
            .WithOne(x => x.RevenueExpenseType)
            .HasForeignKey(x => x.RevenueExpenseTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
