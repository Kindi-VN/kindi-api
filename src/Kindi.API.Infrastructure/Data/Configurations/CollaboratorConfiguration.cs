using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class CollaboratorConfiguration : IEntityTypeConfiguration<Collaborator>
{
    public void Configure(EntityTypeBuilder<Collaborator> builder)
    {
        builder.ToTable("Collaborators");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Zalo)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Position)
            .HasMaxLength(100);

        builder.Property(c => c.Skills)
            .HasMaxLength(500);

        builder.Property(c => c.Interests)
            .HasMaxLength(500);

        builder.Property(c => c.Goals)
            .HasMaxLength(500);

        builder.Property(c => c.Experience)
            .HasMaxLength(1000);

        builder.Property(c => c.CollaboratorCode)
            .HasMaxLength(30);

        builder.Property(c => c.ReferralCode)
            .HasMaxLength(30);

        builder.Property(c => c.RejectionReason)
            .HasMaxLength(500);

        // Cấu hình IsApproved
        builder.Property(c => c.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        // Cấu hình Status (dùng field _status)
        builder.Property(c => c.Status)
            .HasField("_status")
            .UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction)
            .HasConversion<int>()
            .HasDefaultValue(CollaboratorStatus.Pending)
            .HasSentinel(CollaboratorStatus.Pending) 
            .HasColumnName("Status");

        builder.Property(c => c.Level)
            .HasDefaultValue(1);

        builder.Property(c => c.BusinessFieldName)
            .HasMaxLength(200);

        builder.Property(c => c.BusinessName)
            .HasMaxLength(200);

        builder.Property(c => c.Website)
            .HasMaxLength(300);

        // Company relation
        builder.Property(c => c.CompanyId)
            .HasColumnType("uuid");

        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.UserId)
            .IsUnique();

        builder.HasIndex(c => c.Phone)
            .IsUnique();

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        builder.HasIndex(c => c.CollaboratorCode)
            .IsUnique()
            .HasFilter("[CollaboratorCode] IS NOT NULL");

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentCollaborator)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCollaboratorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.BusinessFieldId)
          .HasColumnType("uuid");
    }
}