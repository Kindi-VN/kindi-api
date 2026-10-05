// PartnerConfiguration.cs
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PartnerCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.PartnerCode).IsUnique();

        builder.Property(x => x.Position).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CompanyTax).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CompanyAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CompanyWebsite).HasMaxLength(200);
        // Company relation (centralized company management)
        builder.Property(x => x.CompanyId)
            .HasColumnType("uuid");

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.BusinessType).HasConversion<int>();
        builder.Property(x => x.CompanySize).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>().HasDefaultValue(PartnerStatus.Pending);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}