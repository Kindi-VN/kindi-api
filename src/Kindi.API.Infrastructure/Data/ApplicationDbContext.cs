using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Infrastructure.Data.ModelConfiguration;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kindi.API.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
	private readonly ICurrentUserService _currentUserService;

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
		: base(options)
	{
		_currentUserService = currentUserService;
	}

    DatabaseFacade IApplicationDbContext.Database => Database;
    public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
	public DbSet<GroupBuyingRequest> GroupBuyingRequests { get; set; }
    public DbSet<GroupBuyingParticipant> GroupBuyingParticipants { get; set; }
	public DbSet<OfferRequest> OfferRequests { get; set; }
    public DbSet<Collaborator> Collaborators { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Partner> Partners { get; set; }
	public DbSet<Company> Companies { get; set; }
    public DbSet<PartnerProduct> PartnerProducts { get; set; }
    public DbSet<PartnerCommission> PartnerCommissions { get; set; }
    public DbSet<CommissionConfig> CommissionConfigs { get; set; }
    public DbSet<CommissionTier> CommissionTiers { get; set; }
    public DbSet<SocialPost> SocialPosts { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<BusinessField> BusinessFields { get; set; }
    public DbSet<BusinessGroup> BusinessGroups { get; set; }
    public DbSet<BusinessGroupMember> BusinessGroupMembers { get; set; }
    public DbSet<BusinessGroupPost> BusinessGroupPosts { get; set; }
    public DbSet<BusinessGroupComment> BusinessGroupComments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }
    public DbSet<ReferralEvent> ReferralEvents { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<PermissionGroup> PermissionGroups { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<MembershipTier> MembershipTiers { get; set; }
    public DbSet<UserMembership> UserMemberships { get; set; }
    public DbSet<UserBankAccount> UserBankAccounts { get; set; }
    public DbSet<PayoutSetting> PayoutSettings { get; set; }
    public DbSet<PayoutPeriod> PayoutPeriods { get; set; }
    public DbSet<PayoutStatement> PayoutStatements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Toàn bộ model (entity + cấu hình + global query filter) nằm trong Data/ModelConfiguration.
        // Cấu hình này dùng chung cho cả DbContext ghi và DbContext chỉ đọc.
        modelBuilder.ConfigureKindiModel();
    }

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var currentUserId = _currentUserService.UserId;
		var currentUserName = _currentUserService.UserName;

		// 📝 Audit log: capture TRƯỚC khi soft-delete / gán CreatedBy/UpdatedBy
		// để bắt đúng action Delete (Deleted) và snapshot dữ liệu gốc.
		var auditLogs = AuditLogHelper.CreateAuditLogs(
			ChangeTracker, currentUserId, currentUserName,
			_currentUserService.IpAddress, _currentUserService.UserAgent);

		var entries = ChangeTracker.Entries<BaseEntity>();

		foreach (var entry in entries)
		{
			// Soft delete: convert Deleted to Modified with IsDeleted = true
			if (entry.State == EntityState.Deleted)
			{
				entry.State = EntityState.Modified;
				entry.Entity.IsDeleted = true;
				entry.Entity.UpdatedAt = DateTime.UtcNow;
				entry.Entity.UpdatedBy = currentUserName ?? currentUserId ?? "System";
				continue;
			}

			if (entry.State == EntityState.Added)
			{
				entry.Entity.CreatedAt = DateTime.UtcNow;
				entry.Entity.CreatedBy = currentUserName ?? currentUserId ?? "System";
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Entity.UpdatedAt = DateTime.UtcNow;
				entry.Entity.UpdatedBy = currentUserName ?? currentUserId ?? "System";
			}
		}

		if (auditLogs.Count > 0)
			AuditLogs.AddRange(auditLogs);

		return await base.SaveChangesAsync(cancellationToken);
	}
}