using AutoMapper;
using Kindi.API.Application.Common.Behaviors;
using Kindi.API.Application.Common.Configurations;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Services;
using Kindi.API.Infrastructure.Services;
using Kindi.API.Shared.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System.Reflection;

namespace Kindi.API.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationServices(
		this IServiceCollection services)
	{
        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.AssignableTo<IBusinessFieldService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Add AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

		// Add FluentValidation
		services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

		// JWT Service
		services.AddScoped<IJwtService, JwtService>();

		// Register services
		services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
		services.AddScoped<IGroupBuyingRequestService, GroupBuyingRequestService>();
		services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<ISocialService, SocialService>();
        services.AddScoped<ICollaboratorService, CollaboratorService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IReferralEventService, ReferralEventService>();
        services.AddScoped<ICommissionConfigService, CommissionConfigService>();
        services.AddScoped<IMembershipTierService, MembershipTierService>();
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<ITransactionRevenueService, TransactionRevenueService>();
        services.AddScoped<IRevenueExpenseTypeService, RevenueExpenseTypeService>();
        services.AddScoped<ISocialInteractionService, SocialInteractionService>();
        services.AddScoped<IBusinessFieldService, BusinessFieldService>();
        services.AddScoped<IBusinessGroupService, BusinessGroupService>();
        services.AddScoped<IAuthAuditService, AuthAuditService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IQueryService, QueryService>();

        return services;
	}
}
