using Kindi.API.Application;
using Kindi.API.Application.Common.Configurations;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Resources;
using Kindi.API.Application.Validators;
using Kindi.API.Infrastructure;
using Kindi.API.Infrastructure.Services;
using Kindi.API.WebApi.Configurations;
using Kindi.API.WebApi.Filters;
using Kindi.API.WebApi.Responses;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace Kindi.API.WebApi;

public static class DependencyInjection
{
	public static IServiceCollection AddWebApiServices(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Add JWT Settings
		services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

		// Cấu hình cho điều kiện lọc "chứa một trong các giá trị" (số giá trị tối đa, lọc trùng)
		services.Configure<QueryOptions>(configuration.GetSection(QueryOptions.SectionName));

		var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
		if (jwtSettings == null
			|| string.IsNullOrWhiteSpace(jwtSettings.Secret)
			|| jwtSettings.Secret.Length < 32
			|| string.IsNullOrWhiteSpace(jwtSettings.Issuer)
			|| string.IsNullOrWhiteSpace(jwtSettings.Audience)
			|| jwtSettings.ExpiryMinutes <= 0)
		{
			throw new InvalidOperationException("JWT configuration is invalid. Please configure JwtSettings in appsettings or environment variables.");
		}

		var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

		services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
		.AddJwtBearer(options =>
		{
			options.RequireHttpsMetadata = true;
			options.SaveToken = true;
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = jwtSettings.Issuer,
				ValidateAudience = true,
				ValidAudience = jwtSettings.Audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};

			options.Events = new JwtBearerEvents
			{
				OnChallenge = async context =>
				{
					context.HandleResponse();
					context.Response.StatusCode = 401;
					context.Response.ContentType = "application/json";

					var localizer = context.HttpContext.RequestServices.GetService<IStringLocalizer<SharedResource>>();
					var message = localizer?["UnauthorizedMessage"] ?? "Unauthorized";
					var error = localizer?["AuthenticationRequired"] ?? "Authentication required";

					var response = new ApiResponse<object>
					{
						Success = false,
						Message = message,
						Errors = new List<string> { error },
						Timestamp = DateTime.UtcNow
					};

					await context.Response.WriteAsync(JsonSerializer.Serialize(response));
				},
				OnForbidden = async context =>
				{
					context.Response.StatusCode = 403;
					context.Response.ContentType = "application/json";

					var localizer = context.HttpContext.RequestServices.GetService<IStringLocalizer<SharedResource>>();
					var message = localizer?["ForbiddenMessage"] ?? "Forbidden";
					var error = localizer?["AccessDenied"] ?? "Access denied";

					var response = new ApiResponse<object>
					{
						Success = false,
						Message = message,
						Errors = new List<string> { error },
						Timestamp = DateTime.UtcNow
					};

					await context.Response.WriteAsync(JsonSerializer.Serialize(response));
				}
			};
		});

		services.AddAuthorization();

		// Add API Versioning
		services.AddApiVersioningConfig();

		// Add Application layer services
		services.AddApplicationServices();

		// Add Infrastructure layer services
		services.AddInfrastructureServices(configuration);

		// File storage (local disk). Muốn dùng S3/Cloudinary: thay impl này + giữ nguyên IFileStorage.
		services.AddScoped<IFileStorage>(sp =>
		{
			var env = sp.GetRequiredService<IWebHostEnvironment>();
			var root = Path.Combine(Path.GetFullPath(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot")));
			Directory.CreateDirectory(root);
			return new LocalFileStorage(root);
		});

		// Add API specific services with FluentValidation
		services.AddControllers(options =>
		{
			options.Filters.Add<ValidationFilter>();
		});
		services.AddValidatorsFromAssemblyContaining<SharedResource>();
		services.AddFluentValidationAutoValidation();
		services.AddFluentValidationClientsideAdapters();

		// QUAN TRỌNG: Tắt filter mặc định của .NET
		services.Configure<ApiBehaviorOptions>(options =>
		{
			options.SuppressModelStateInvalidFilter = true;
		});

		services.AddLocalization();

		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();

		return services;
	}
}