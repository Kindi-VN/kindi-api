using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class UserMembershipConfiguration : IEntityTypeConfiguration<UserMembership>
{
    public void Configure(EntityTypeBuilder<UserMembership> builder)
    {
        builder.ToTable("UserMemberships");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccumulatedValue).HasPrecision(18, 2);

        // Mỗi tài khoản một bản ghi hạng; kiểm tra trùng ở tầng service (bảng có xoá mềm).
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MembershipTier)
            .WithMany()
            .HasForeignKey(x => x.MembershipTierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
