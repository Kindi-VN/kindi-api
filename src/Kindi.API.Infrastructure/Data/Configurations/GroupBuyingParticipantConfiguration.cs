using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

/// <summary>
/// Cấu hình bảng người tham gia mua chung.
/// NOTE: GroupBuyingRequestConfiguration.cs bị exclude khỏi compile (xem csproj) nên
/// quan hệ 1-n giữa GroupBuyingRequest và participant được khai báo ở đây.
/// </summary>
public class GroupBuyingParticipantConfiguration : IEntityTypeConfiguration<GroupBuyingParticipant>
{
    public void Configure(EntityTypeBuilder<GroupBuyingParticipant> builder)
    {
        builder.ToTable("GroupBuyingParticipants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.GroupBuyingParticipantCode)
            .HasMaxLength(30);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.RecordReferrerCode)
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(GroupBuyingParticipantStatus.Joined);

        builder.HasOne(x => x.GroupBuyingRequest)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.GroupBuyingRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi user chỉ có 1 bản ghi trong 1 nhóm (hủy rồi tham gia lại thì tái kích hoạt bản ghi cũ).
        builder.HasIndex(x => new { x.GroupBuyingRequestId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => x.GroupBuyingParticipantCode)
            .IsUnique()
            .HasFilter("[GroupBuyingParticipantCode] IS NOT NULL");
    }
}
