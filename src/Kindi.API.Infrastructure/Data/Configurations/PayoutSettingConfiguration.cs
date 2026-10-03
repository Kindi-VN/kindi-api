using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class PayoutSettingConfiguration : IEntityTypeConfiguration<PayoutSetting>
{
    public void Configure(EntityTypeBuilder<PayoutSetting> builder)
    {
        builder.ToTable("PayoutSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EarlyWithdrawalFeeRate).HasPrecision(5, 2);
        builder.Property(x => x.MinEarlyWithdrawalFee).HasPrecision(18, 2);
        builder.Property(x => x.MaxEarlyWithdrawalFee).HasPrecision(18, 2);
        builder.Property(x => x.MinWithdrawalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(500);
    }
}
