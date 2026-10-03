namespace Kindi.API.Infrastructure.Data.Configurations;

using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(8);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .HasMaxLength(200);

        builder.Property(x => x.Endpoints)
            .HasMaxLength(1000);

        builder.Property(x => x.ParentCode)
            .HasMaxLength(50);

        builder.HasIndex(x => x.ParentCode);

        // Xoá nhóm không xoá quyền — quyền chỉ mất liên kết nhóm.
        builder.HasOne(x => x.ParentGroup)
            .WithMany()
            .HasForeignKey(x => x.ParentCode)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
