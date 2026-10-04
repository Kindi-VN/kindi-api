using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class BusinessGroupMemberConfiguration : IEntityTypeConfiguration<BusinessGroupMember>
{
    public void Configure(EntityTypeBuilder<BusinessGroupMember> builder)
    {
        builder.ToTable("BusinessGroupMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessGroupMemberCode)
            .HasMaxLength(30);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.RecordReferrerCode)
            .HasMaxLength(30);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .HasDefaultValue(GroupMemberRole.Member);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(GroupMemberStatus.Pending);

        builder.HasOne(x => x.BusinessGroup)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.BusinessGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi user chỉ có 1 bản ghi thành viên trong 1 nhóm (bị từ chối/rời nhóm thì tái kích hoạt bản ghi cũ).
        builder.HasIndex(x => new { x.BusinessGroupId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.BusinessGroupMemberCode)
            .IsUnique()
            .HasFilter("[BusinessGroupMemberCode] IS NOT NULL");
    }
}
