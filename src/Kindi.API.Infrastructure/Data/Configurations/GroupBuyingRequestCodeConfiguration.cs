using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

/// <summary>
/// Config riêng cho các cột của GroupBuyingRequests.
/// NOTE: GroupBuyingRequestConfiguration.cs bị exclude khỏi compile (xem csproj),
/// nên config cột được khai báo ở file riêng để đảm bảo được áp dụng.
/// </summary>
public class GroupBuyingRequestCodeConfiguration : IEntityTypeConfiguration<GroupBuyingRequest>
{
    public void Configure(EntityTypeBuilder<GroupBuyingRequest> builder)
    {
        builder.ToTable("GroupBuyingRequests");

        builder.Property(x => x.GroupBuyingRequestCode)
            .HasMaxLength(30);

        builder.Property(x => x.RecordReferrerCode)
            .HasMaxLength(30);

        builder.HasIndex(x => x.GroupBuyingRequestCode)
            .IsUnique()
            .HasFilter("[GroupBuyingRequestCode] IS NOT NULL");
    }
}