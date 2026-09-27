using DotNetEnv;
using Kindi.API.Infrastructure.Data;
using Kindi.API.WebApi;
using Kindi.API.WebApi.Configurations;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TelegramBot;
using Swashbuckle.AspNetCore.SwaggerGen;

try
{
    // Bootstrap logger: ghi được cả những log phát ra TRƯỚC khi cấu hình xong Serilog
    // (đọc .env, connection string, quyết định bật/tắt sink Seq...). Bị thay thế khi gọi UseSerilog().
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("🚀 Starting Kindi API...");

    // Load .env
    LoadEnvironmentFile();

    // wwwroot không có trong git: phải tạo TRƯỚC khi tạo builder và khai báo ngay trong WebApplicationOptions.
    // (Gọi builder.WebHost.UseWebRoot() sau đó sẽ ném NotSupportedException: "The web root changed from ..."
    //  vì với WebApplicationBuilder không được đổi cấu hình host sau khi đã tạo.)
    var contentRoot = Directory.GetCurrentDirectory();
    var webRootPath = Path.Combine(contentRoot, "wwwroot");
    Directory.CreateDirectory(webRootPath);

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        WebRootPath = webRootPath
    });

    Log.Information("✅ Web root ready: {WebRootPath}", webRootPath);

    ConfigureServices(builder);

    var app = builder.Build();
    await ConfigurePipeline(app);
    //var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    //Log.Information("🔗 Connection String: {ConnectionString}", connStr);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "🔥 Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ===== Methods =====

static void LoadEnvironmentFile()
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
    envPath = Path.GetFullPath(envPath);

    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        Console.WriteLine("✅ Environment file loaded");
        Log.Information("Environment file loaded from: {Path}", envPath);
    }
    else
    {
        Console.WriteLine("⚠️ Environment file not found; using OS environment variables");
        Log.Warning("Environment file not found at: {Path}", envPath);
    }
}

static string? GetEnvironmentValue(string key)
{
    var value = Environment.GetEnvironmentVariable(key);
    if (!string.IsNullOrWhiteSpace(value))
    {
        Log.Debug("🔑 Found {Key} from environment variable", key);
        return value;
    }

    try
    {
        value = Env.GetString(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            Log.Debug("🔑 Found {Key} from .env file", key);
        }
        return value;
    }
    catch
    {
        Log.Debug("❌ {Key} not found in .env file", key);
        return null;
    }
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    Log.Information("⚙️ Configuring services...");

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    builder.Services.AddLogging();

    // Load appsettings mặc định
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

    // Chỉ Production mới dùng Environment Variables
    if (builder.Environment.IsProduction())
    {
        Log.Information("📋 Loading Production environment variables...");

        var envConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = GetEnvironmentValue("DB_CONNECTION_STRING") ?? GetEnvironmentValue("DATABASE_URL"),
            ["ConnectionStrings:ReadConnection"] = GetEnvironmentValue("READ_CONNECTION_STRING"),
            ["Query:MaxContainsValues"] = GetEnvironmentValue("QUERY_MAX_CONTAINS_VALUES"),
            ["JwtSettings:Secret"] = GetEnvironmentValue("JWT_SECRET"),
            ["JwtSettings:Issuer"] = GetEnvironmentValue("JWT_ISSUER"),
            ["JwtSettings:Audience"] = GetEnvironmentValue("JWT_AUDIENCE"),
            ["JwtSettings:ExpiryMinutes"] = GetEnvironmentValue("JWT_EXPIRY_MINUTES"),
            ["Logging:LogLevel:Default"] = GetEnvironmentValue("LOG_LEVEL"),
            ["CorsSettings:Policy"] = GetEnvironmentValue("CORS_POLICY"),
            ["CorsSettings:AllowedOrigins"] = GetEnvironmentValue("ALLOWED_ORIGINS")
        };

        builder.Configuration
            .AddInMemoryCollection(envConfig.Where(x => x.Value != null)
                .ToDictionary(x => x.Key, x => x.Value))
            .AddEnvironmentVariables();
    }
    else
    {
        Log.Information("📋 Using appsettings.{Environment}.json for {Environment}",
            builder.Environment.EnvironmentName, builder.Environment.EnvironmentName);
    }

    // Log connection string để debug (ẩn password)
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connStr))
    {
        var masked = connStr.Contains("Password=")
            ? connStr.Replace(connStr.Split("Password=")[1].Split(';')[0], "*****")
            : connStr;
        Log.Information("🔗 Connection String: {ConnectionString}", masked);
    }

    // DbContext chỉ đọc dùng connection string riêng nếu có (replica), không thì dùng chung DB ghi
    var readConnStr = builder.Configuration.GetConnectionString("ReadConnection");
    Log.Information("📚 Read connection: {ReadConnection}",
        string.IsNullOrWhiteSpace(readConnStr) ? "(chưa cấu hình → đọc chung DB ghi)" : "(đã cấu hình ReadConnection)");

    // Serilog
    Log.Information("📝 Configuring Serilog...");

    // Seq — log tập trung: mặc định luôn bắn về Seq chạy local (http://localhost:5341) ở mọi môi trường.
    // Đổi đích bằng SEQ_SERVER_URL / Serilog:Seq:ServerUrl (+ SEQ_API_KEY khi Seq bật xác thực);
    // tắt sink bằng SEQ_ENABLED=false / Serilog:Seq:Enabled=false.
    // ServerUrl rỗng + Enabled=false ⇒ bỏ sink, app vẫn chạy và vẫn ghi console + file.
    const string localSeqUrl = "http://localhost:5341";

    var seqUrl = GetEnvironmentValue("SEQ_SERVER_URL");
    if (string.IsNullOrWhiteSpace(seqUrl)) seqUrl = builder.Configuration["Serilog:Seq:ServerUrl"];
    if (string.IsNullOrWhiteSpace(seqUrl)) seqUrl = localSeqUrl;

    var seqApiKey = GetEnvironmentValue("SEQ_API_KEY") ?? builder.Configuration["Serilog:Seq:ApiKey"];
    var seqMinimumLevel = builder.Configuration.GetValue("Serilog:Seq:MinimumLevel", LogEventLevel.Information);

    // SEQ_ENABLED có mặt thì quyết định hoàn toàn (bật/tắt được cả khi appsettings nói khác),
    // không có thì lấy theo Serilog:Seq:Enabled.
    var seqEnabledSetting = GetEnvironmentValue("SEQ_ENABLED");
    var seqEnabled = seqEnabledSetting != null
        ? !string.Equals(seqEnabledSetting, "false", StringComparison.OrdinalIgnoreCase)
        : builder.Configuration.GetValue("Serilog:Seq:Enabled", true);

    // Đọc từ appsettings trước, fallback sang env variable
    var telegramToken = builder.Configuration["Telegram:ErrorBot:BotToken"]
                        ?? builder.Configuration["TELEGRAM_BOT_TOKEN"]
                        ?? throw new Exception("Telegram ErrorBot Token is required");

    var chatId = builder.Configuration["Telegram:ErrorBot:ChatId"]
                 ?? builder.Configuration["TELEGRAM_CHAT_ID"]
                 ?? throw new Exception("Telegram ErrorBot ChatId is required");

    var loggerConfiguration = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Kindi.API")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day);

    if (seqEnabled && !string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(
            seqUrl,
            apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey,
            restrictedToMinimumLevel: seqMinimumLevel);

        Log.Information("📡 Seq sink: {SeqUrl} (mức tối thiểu {MinimumLevel})", seqUrl, seqMinimumLevel);
    }
    else
    {
        Log.Information("📡 Seq sink: tắt (Serilog:Seq:ServerUrl trống hoặc Enabled=false)");
    }

    loggerConfiguration.WriteTo.TelegramBot(telegramToken, chatId,
        applicationName: "Kindi.API",
        renderMessageImplementation: TelegramMessageFormatter.Build,
        restrictedToMinimumLevel: LogEventLevel.Error,
        parseMode: ParseMode.HTML);

    Log.Logger = loggerConfiguration.CreateLogger();

    builder.Host.UseSerilog();

    // Services
    Log.Information("🔧 Adding WebApi services...");
    builder.Services.AddWebApiServices(builder.Configuration);
    builder.Services.AddHealthChecks();

    ConfigureCors(builder);
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

    Log.Information("✅ Services configured successfully");
}

static void ConfigureCors(WebApplicationBuilder builder)
{
    var allowedOriginsRaw = GetEnvironmentValue("ALLOWED_ORIGINS");
    var useAllowAllOrigins = string.IsNullOrWhiteSpace(allowedOriginsRaw) || allowedOriginsRaw.Trim() == "*";

    Log.Information("🔒 Configuring CORS...");
    Log.Information("🔒 UseAllowAllOrigins: {UseAllowAllOrigins}", useAllowAllOrigins);
    if (!useAllowAllOrigins)
    {
        Log.Information("🔒 AllowedOrigins: {AllowedOrigins}", allowedOriginsRaw);
    }

    builder.Services.AddCors(options =>
    {
        if (useAllowAllOrigins)
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
            Log.Information("🔒 CORS policy 'AllowAllOrigins' configured");
        }
        else
        {
            var allowedOrigins = allowedOriginsRaw!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            options.AddPolicy("AllowSpecific", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
            Log.Information("🔒 CORS policy 'AllowSpecific' configured with {Count} origins", allowedOrigins.Length);
        }

        options.AddPolicy("SwaggerPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
        Log.Information("🔒 CORS policy 'SwaggerPolicy' configured");
    });
}

static async Task ConfigurePipeline(WebApplication app)
{
    Log.Information("🚀 Configuring pipeline...");
    Log.Information("🌍 Application running in: {Environment}", app.Environment.EnvironmentName);

    await RunDatabaseMigration(app);
    ConfigureLocalization(app);
    ConfigureSwagger(app);
    ConfigureMiddleware(app);
    ConfigureEndpoints(app);

    Log.Information("✅ Pipeline configured successfully");
}

static async Task RunDatabaseMigration(WebApplication app)
{
    var environment = app.Environment.EnvironmentName;
    Log.Information("📦 Checking database migration for environment: {Environment}", environment);

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    if (app.Environment.IsStaging() || app.Environment.IsProduction())
    {
        Log.Information("🔄 Running migrations for {Environment}...", environment);
        try
        {
            await DatabaseMigrationRunner.ApplyMigrationsAsync(db, migrationLogger);
            Log.Information("✅ Migrations applied successfully for {Environment}", environment);

            await DatabaseSeeder.SeedAsync(db);
            Log.Information("✅ Seed data applied successfully for {Environment}", environment);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Failed to apply migrations/seed for {Environment}", environment);
            throw;
        }
    }
    else
    {
        Log.Information("⏭️ Skipping migrations for {Environment} (not staging/production)", environment);
    }
}

static void ConfigureLocalization(WebApplication app)
{
    Log.Information("🌐 Configuring localization...");

    var supportedCultures = new[] { "en", "vi" };
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture("vi")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    app.UseRequestLocalization(localizationOptions);

    Log.Information("🌐 Localization configured with cultures: {Cultures}", string.Join(", ", supportedCultures));
}

static void ConfigureSwagger(WebApplication app)
{
    // Đọc từ appsettings (có thể override bằng environment variable)
    var enableSwagger = app.Configuration.GetValue<bool>("EnableSwagger", false);
    var isDevelopment = app.Environment.IsDevelopment();

    Log.Information("📝 Swagger configuration - IsDevelopment: {IsDevelopment}, EnableSwagger: {EnableSwagger}",
        isDevelopment, enableSwagger);

    if (isDevelopment || enableSwagger)
    {
        Log.Information("📝 Enabling Swagger...");
        try
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
            });
            Log.Information("✅ Swagger enabled successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Failed to configure Swagger");
            throw;
        }
    }
    else
    {
        Log.Information("⏭️ Skipping Swagger (not development and EnableSwagger not true)");
    }
}

static void ConfigureMiddleware(WebApplication app)
{
    Log.Information("🔧 Configuring middleware...");

    app.UseSerilogRequestLogging();
    Log.Information("✅ Serilog request logging configured");

    // Phục vụ file tĩnh (ảnh upload): wwwroot
    app.UseStaticFiles();
    Log.Information("✅ Static files (wwwroot) configured");

    ConfigureCorsMiddleware(app);

    app.UseMiddleware<Kindi.API.WebApi.Middlewares.GlobalExceptionMiddleware>();
    Log.Information("✅ GlobalExceptionMiddleware configured");

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
        Log.Information("✅ HTTPS redirection configured");
    }

    app.UseAuthentication();
    app.UseAuthorization();
    Log.Information("✅ Authentication & Authorization configured");
}

static void ConfigureCorsMiddleware(WebApplication app)
{
    var allowedOriginsRaw = GetEnvironmentValue("ALLOWED_ORIGINS");
    var useAllowAllOrigins = string.IsNullOrWhiteSpace(allowedOriginsRaw) || allowedOriginsRaw.Trim() == "*";

    if (app.Environment.IsDevelopment())
    {
        app.UseCors("SwaggerPolicy");
        Log.Information("🔒 Using SwaggerPolicy CORS for Development");
    }
    else
    {
        var corsPolicy = useAllowAllOrigins
            ? "AllowAllOrigins"
            : GetEnvironmentValue("CORS_POLICY") ?? "AllowSpecific";
        app.UseCors(corsPolicy);
        Log.Information("🔒 Using CORS policy: {CorsPolicy} for {Environment}", corsPolicy, app.Environment.EnvironmentName);
    }
}

static void ConfigureEndpoints(WebApplication app)
{
    Log.Information("📍 Configuring endpoints...");

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("✅ Endpoints configured: Controllers, HealthCheck at /health");
}

// ===== Entry point =====

public partial class Program
{
    // Empty constructor is required for startup
    protected Program() { }

}