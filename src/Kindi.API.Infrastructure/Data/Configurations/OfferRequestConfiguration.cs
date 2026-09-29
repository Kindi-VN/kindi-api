using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class OfferRequestConfiguration : IEntityTypeConfiguration<OfferRequest>
{
    public void Configure(EntityTypeBuilder<OfferRequest> builder)
    {
        builder.ToTable("OfferRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ProductLink)
            .HasMaxLength(500);

        builder.Property(x => x.CurrentPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.ExpectedPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.ReferralCode)
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(OfferStatus.Pending);

        builder.Property(x => x.IsOfferSent)
            .HasDefaultValue(false);

        builder.Property(x => x.OfferRequestCode)
            .HasMaxLength(30);

        builder.HasIndex(x => x.OfferRequestCode)
            .IsUnique()
            .HasFilter("[OfferRequestCode] IS NOT NULL");

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}