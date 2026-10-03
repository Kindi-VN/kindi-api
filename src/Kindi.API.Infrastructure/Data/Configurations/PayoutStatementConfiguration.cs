using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class PayoutStatementConfiguration : IEntityTypeConfiguration<PayoutStatement>
{
    public void Configure(EntityTypeBuilder<PayoutStatement> builder)
    {
        builder.ToTable("PayoutStatements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.AccruedAmount).HasPrecision(18, 2);
        builder.Property(x => x.FeeRate).HasPrecision(5, 2);
        builder.Property(x => x.FeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.BankName).HasMaxLength(150);
        builder.Property(x => x.BankBranch).HasMaxLength(150);
        builder.Property(x => x.BankAccountNumber).HasMaxLength(50);
        builder.Property(x => x.BankAccountHolder).HasMaxLength(150);
        builder.Property(x => x.ProcessedBy).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.PayoutPeriodId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PayoutPeriod)
            .WithMany(x => x.Statements)
            .HasForeignKey(x => x.PayoutPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
