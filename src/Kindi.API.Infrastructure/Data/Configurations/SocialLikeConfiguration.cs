using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

/// <summary>Cấu hình bảng lượt thích bài viết.</summary>
public class SocialLikeConfiguration : IEntityTypeConfiguration<SocialLike>
{
    public void Configure(EntityTypeBuilder<SocialLike> builder)
    {
        builder.ToTable("SocialLike");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SocialLikeCode)
            .HasMaxLength(30);

        builder.HasIndex(x => x.SocialLikeCode)
            .IsUnique()
            .HasFilter("[SocialLikeCode] IS NOT NULL");
    }
}
