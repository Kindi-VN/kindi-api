using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class UserBankAccountConfiguration : IEntityTypeConfiguration<UserBankAccount>
{
    public void Configure(EntityTypeBuilder<UserBankAccount> builder)
    {
        builder.ToTable("UserBankAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Branch).HasMaxLength(150);
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccountHolder).HasMaxLength(150).IsRequired();
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);

        // Mỗi tài khoản một thông tin ngân hàng; kiểm tra trùng ở tầng service (bảng có xoá mềm).
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
