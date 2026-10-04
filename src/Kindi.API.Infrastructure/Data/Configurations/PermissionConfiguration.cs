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
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NameKey)
            .HasMaxLength(100);

        builder.Property(x => x.Route)
            .HasMaxLength(200);

        builder.Property(x => x.Endpoints)
            .HasMaxLength(1000);

        builder.Property(x => x.ParentCode)
            .HasMaxLength(50);

        builder.HasIndex(x => x.ParentCode);

        // Cây quyền tự tham chiếu: cha là một node khác trong chính bảng Permissions (ParentCode → Code).
        // Xoá node cha không xoá con (Restrict) — tránh mất dữ liệu gán quyền.
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCode)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
