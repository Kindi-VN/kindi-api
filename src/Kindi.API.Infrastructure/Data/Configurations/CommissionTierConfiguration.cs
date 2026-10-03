using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class CommissionTierConfiguration : IEntityTypeConfiguration<CommissionTier>
{
    public void Configure(EntityTypeBuilder<CommissionTier> builder)
    {
        builder.ToTable("CommissionTiers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromValue).HasPrecision(18, 2);
        builder.Property(x => x.ToValue).HasPrecision(18, 2);
        builder.Property(x => x.Rate).HasPrecision(18, 2);

        builder.HasIndex(x => x.CommissionConfigId);
    }
}
