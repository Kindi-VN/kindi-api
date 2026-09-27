using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class BusinessGroupCommentConfiguration : IEntityTypeConfiguration<BusinessGroupComment>
{
    public void Configure(EntityTypeBuilder<BusinessGroupComment> builder)
    {
        builder.ToTable("BusinessGroupComments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessGroupCommentCode)
            .HasMaxLength(30);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.BusinessGroupPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentComment)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BusinessGroupPostId, x.CreatedAt });

        builder.HasIndex(x => x.BusinessGroupCommentCode)
            .IsUnique()
            .HasFilter("[BusinessGroupCommentCode] IS NOT NULL");
    }
}
