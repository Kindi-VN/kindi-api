using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class MembershipTierConfiguration : IEntityTypeConfiguration<MembershipTier>
{
    public void Configure(EntityTypeBuilder<MembershipTier> builder)
    {
        builder.ToTable("MembershipTiers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Level).HasConversion<int>();
        builder.Property(x => x.MinAccumulatedValue).HasPrecision(18, 2);
        builder.Property(x => x.EarlyWithdrawalFeeRate).HasPrecision(5, 2);
        builder.Property(x => x.MonthlyWithdrawalLimit).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Level);
        builder.HasIndex(x => x.MinAccumulatedValue);
    }
}
