using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class BusinessGroupPostConfiguration : IEntityTypeConfiguration<BusinessGroupPost>
{
    public void Configure(EntityTypeBuilder<BusinessGroupPost> builder)
    {
        builder.ToTable("BusinessGroupPosts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessGroupPostCode)
            .HasMaxLength(30);

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.RefCode)
            .HasMaxLength(30);

        builder.Property(x => x.ReferralCode)
            .HasMaxLength(30);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .HasDefaultValue(GroupPostType.Discussion);

        builder.HasOne(x => x.BusinessGroup)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.BusinessGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessGroupId, x.CreatedAt });
        builder.HasIndex(x => x.IsPrivateToAdmin);
    }
}
