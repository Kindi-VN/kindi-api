using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kindi.API.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("Users");

		builder.HasKey(x => x.Id);

		builder.Property(x => x.Username)
			.IsRequired()
			.HasMaxLength(50);

		builder.HasIndex(x => x.Username)
			.IsUnique();

		builder.Property(x => x.PasswordHash)
			.HasMaxLength(255)
			.IsRequired(false); // Cho phép null

		builder.Property(x => x.FullName)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(x => x.Email)
			.IsRequired()
			.HasMaxLength(100);

		builder.HasIndex(x => x.Email)
			.IsUnique();

		builder.Property(x => x.Phone)
			.HasMaxLength(20);

		builder.Property(x => x.Zalo)
			.HasMaxLength(20);

		builder.HasIndex(x => x.Phone)
			.IsUnique()
			.HasDatabaseName("IX_Users_Phone_Unique");

		builder.Property(x => x.Role)
			.HasConversion<int>()
			.HasDefaultValue(UserRole.Customer);

		builder.Property(x => x.IsActive)
			.HasDefaultValue(true);

		builder.Property(x => x.ReferralCode)
			.HasMaxLength(30);

		builder.Property(x => x.ReferredByCode)
			.HasMaxLength(30);

		builder.HasIndex(x => x.ReferredByCode);

		builder.Property(x => x.MustChangeCredentials)
			.HasDefaultValue(false);

		builder.Property(x => x.UserCode)
			.HasMaxLength(30);

		builder.HasIndex(x => x.UserCode)
			.IsUnique()
			.HasFilter("[UserCode] IS NOT NULL");
	}
}