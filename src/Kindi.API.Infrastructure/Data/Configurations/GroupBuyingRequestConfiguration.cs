using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class GroupBuyingRequestConfiguration : IEntityTypeConfiguration<GroupBuyingRequest>
{
    public void Configure(EntityTypeBuilder<GroupBuyingRequest> builder)
    {
        builder.ToTable("GroupBuyingRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ProductLink)
            .HasMaxLength(500);

        builder.Property(x => x.TargetPeopleCount)
            .IsRequired();

        builder.Property(x => x.CurrentPeopleCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.TargetPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(x => x.Zalo)
            .HasMaxLength(15);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(GroupBuyingStatus.Pending);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}