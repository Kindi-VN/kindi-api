using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class PayoutPeriodConfiguration : IEntityTypeConfiguration<PayoutPeriod>
{
    public void Configure(EntityTypeBuilder<PayoutPeriod> builder)
    {
        builder.ToTable("PayoutPeriods");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.Year, x.Month });
    }
}
