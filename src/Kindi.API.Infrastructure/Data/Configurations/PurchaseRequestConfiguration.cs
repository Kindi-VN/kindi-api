using Kindi.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProductCategory)
            .HasMaxLength(100); 

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50); 

        builder.Property(x => x.ExpectedPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.RecordReferrerCode)
            .HasMaxLength(30);

        builder.Property(x => x.AdminNote)
            .HasMaxLength(500);

        builder.Property(x => x.Source)
            .HasMaxLength(50);

        builder.Property(x => x.PurchaseRequestCode)
            .HasMaxLength(30);

        builder.HasIndex(x => x.PurchaseRequestCode)
            .IsUnique()
            .HasFilter("[PurchaseRequestCode] IS NOT NULL");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.PurchaseRequestStatus.Pending);

        // Relationship
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}