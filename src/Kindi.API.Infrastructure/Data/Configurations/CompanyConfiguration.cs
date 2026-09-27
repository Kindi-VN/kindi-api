using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CompanyCode)
            .HasMaxLength(30);

        builder.HasIndex(c => c.CompanyCode)
            .IsUnique()
            .HasFilter("\"CompanyCode\" IS NOT NULL");

        builder.Property(c => c.TaxCode)
            .HasMaxLength(50);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Website)
            .HasMaxLength(300);

        builder.Property(c => c.BusinessFieldId)
            .HasColumnType("uuid");
    }
}
