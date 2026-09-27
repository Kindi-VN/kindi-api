using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

/// <summary>Cấu hình bảng lượt chia sẻ bài viết.</summary>
public class SocialShareConfiguration : IEntityTypeConfiguration<SocialShare>
{
    public void Configure(EntityTypeBuilder<SocialShare> builder)
    {
        builder.ToTable("SocialShare");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SocialShareCode)
            .HasMaxLength(30);

        builder.HasIndex(x => x.SocialShareCode)
            .IsUnique()
            .HasFilter("[SocialShareCode] IS NOT NULL");
    }
}
