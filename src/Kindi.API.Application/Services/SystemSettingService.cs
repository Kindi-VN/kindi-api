namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cấu hình chung của hệ thống. Toàn hệ thống dùng chung một bản ghi; chưa cấu hình lần nào thì
/// đọc ra giá trị mặc định và bản ghi chỉ được tạo ở lần lưu đầu tiên.
/// </summary>
public sealed class SystemSettingService : ISystemSettingService
{
    private static readonly string[] SupportedLanguages = ["vi", "en"];

    private readonly IRepository<SystemSetting> _settingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<SystemSettingService> _logger;

    public SystemSettingService(
        IRepository<SystemSetting> settingRepository,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<SystemSettingService> logger)
    {
        _settingRepository = settingRepository;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SystemSettingResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetFirstAsync(x => !x.IsDeleted, cancellationToken)
                      ?? new SystemSetting();

        return Map(setting);
    }

    /// <inheritdoc />
    public async Task<PublicSystemSettingResponse> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetFirstAsync(x => !x.IsDeleted, cancellationToken)
                      ?? new SystemSetting();

        return new PublicSystemSettingResponse
        {
            SystemName = setting.SystemName,
            SupportEmail = setting.SupportEmail,
            SupportPhone = setting.SupportPhone,
            Address = setting.Address,
            WorkingHours = setting.WorkingHours,
            FacebookUrl = setting.FacebookUrl,
            YoutubeUrl = setting.YoutubeUrl,
            ZaloUrl = setting.ZaloUrl,
            CopyrightText = setting.CopyrightText,
            DefaultLanguage = setting.DefaultLanguage,
            TimeZone = setting.TimeZone,
            CurrencySymbol = setting.CurrencySymbol,
            DateFormat = setting.DateFormat,
            AllowRegistration = setting.AllowRegistration
        };
    }

    /// <inheritdoc />
    public async Task<SystemSettingResponse> SaveAsync(SaveSystemSettingRequest request, CancellationToken cancellationToken = default)
    {
        var systemName = request.SystemName?.Trim();
        if (string.IsNullOrWhiteSpace(systemName))
            throw new BusinessException(_localizer["SystemSetting_NameRequired"]);

        if (!SupportedLanguages.Contains(request.DefaultLanguage?.Trim().ToLowerInvariant()))
            throw new BusinessException(_localizer["SystemSetting_LanguageInvalid"]);

        if (request.AccessTokenMinutes is < 5 or > 1440 || request.RefreshTokenDays is < 1 or > 365)
            throw new BusinessException(_localizer["SystemSetting_TokenInvalid"]);

        if (request.MinPasswordLength is < 6 or > 64)
            throw new BusinessException(_localizer["SystemSetting_PasswordLengthInvalid"]);

        if (request.MaxFailedLoginAttempts is < 1 or > 20 || request.LockoutMinutes is < 1 or > 1440)
            throw new BusinessException(_localizer["SystemSetting_LockoutInvalid"]);

        var referralPrefix = request.ReferralCodePrefix?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(referralPrefix) || request.ReferralCodeLength is < 4 or > 12)
            throw new BusinessException(_localizer["SystemSetting_ReferralCodeInvalid"]);

        if (request.CommissionAttributionDays is < 0 or > 365)
            throw new BusinessException(_localizer["SystemSetting_AttributionInvalid"]);

        if (request.MaxUploadSizeMb is < 1 or > 200 || request.MaxImagesPerPost is < 0 or > 50)
            throw new BusinessException(_localizer["SystemSetting_UploadInvalid"]);

        if (request.AuditLogRetentionDays is < 0 or > 3650)
            throw new BusinessException(_localizer["SystemSetting_AuditRetentionInvalid"]);

        if (string.IsNullOrWhiteSpace(request.DateFormat?.Trim()) || string.IsNullOrWhiteSpace(request.TimeZone?.Trim()))
            throw new BusinessException(_localizer["SystemSetting_FormatInvalid"]);

        var setting = await _settingRepository.GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => !x.IsDeleted, cancellationToken);

        var isNew = setting == null;
        setting ??= new SystemSetting();

        setting.SystemName = systemName!;
        setting.SupportEmail = request.SupportEmail?.Trim();
        setting.SupportPhone = request.SupportPhone?.Trim();
        setting.Address = request.Address?.Trim();
        setting.WorkingHours = request.WorkingHours?.Trim();
        setting.FacebookUrl = request.FacebookUrl?.Trim();
        setting.YoutubeUrl = request.YoutubeUrl?.Trim();
        setting.ZaloUrl = request.ZaloUrl?.Trim();
        setting.CopyrightText = request.CopyrightText?.Trim();

        setting.DefaultLanguage = request.DefaultLanguage!.Trim().ToLowerInvariant();
        setting.TimeZone = request.TimeZone!.Trim();
        setting.CurrencySymbol = request.CurrencySymbol?.Trim() ?? string.Empty;
        setting.DateFormat = request.DateFormat!.Trim();

        setting.AllowRegistration = request.AllowRegistration;
        setting.RequireEmailVerification = request.RequireEmailVerification;
        setting.MinPasswordLength = request.MinPasswordLength;
        setting.AccessTokenMinutes = request.AccessTokenMinutes;
        setting.RefreshTokenDays = request.RefreshTokenDays;
        setting.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts;
        setting.LockoutMinutes = request.LockoutMinutes;
        setting.ReferralCodePrefix = referralPrefix;
        setting.ReferralCodeLength = request.ReferralCodeLength;
        setting.CommissionAttributionDays = request.CommissionAttributionDays;

        setting.MaxUploadSizeMb = request.MaxUploadSizeMb;
        setting.AllowedImageExtensions = request.AllowedImageExtensions?.Trim();
        setting.AllowedDocumentExtensions = request.AllowedDocumentExtensions?.Trim();
        setting.MaxImagesPerPost = request.MaxImagesPerPost;
        setting.RequirePostApproval = request.RequirePostApproval;
        setting.RequireGroupApproval = request.RequireGroupApproval;
        setting.AuditLogRetentionDays = request.AuditLogRetentionDays;
        setting.EnableEmailNotification = request.EnableEmailNotification;
        setting.NotificationSenderName = request.NotificationSenderName?.Trim();
        setting.NotificationReplyTo = request.NotificationReplyTo?.Trim();

        setting.Note = request.Note?.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = _currentUserService.UserName;

        if (isNew)
        {
            setting.Id = Guid.NewGuid();
            setting.CreatedAt = DateTime.UtcNow;
            setting.CreatedBy = _currentUserService.UserName;
            await _settingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            _settingRepository.Update(setting);
        }

        await _settingRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã cập nhật cài đặt chung: {SystemName} — ngôn ngữ {Language}", setting.SystemName, setting.DefaultLanguage);

        return Map(setting);
    }

    private static SystemSettingResponse Map(SystemSetting setting) => new()
    {
        SystemName = setting.SystemName,
        SupportEmail = setting.SupportEmail,
        SupportPhone = setting.SupportPhone,
        Address = setting.Address,
        WorkingHours = setting.WorkingHours,
        FacebookUrl = setting.FacebookUrl,
        YoutubeUrl = setting.YoutubeUrl,
        ZaloUrl = setting.ZaloUrl,
        CopyrightText = setting.CopyrightText,
        DefaultLanguage = setting.DefaultLanguage,
        TimeZone = setting.TimeZone,
        CurrencySymbol = setting.CurrencySymbol,
        DateFormat = setting.DateFormat,
        AllowRegistration = setting.AllowRegistration,
        RequireEmailVerification = setting.RequireEmailVerification,
        MinPasswordLength = setting.MinPasswordLength,
        AccessTokenMinutes = setting.AccessTokenMinutes,
        RefreshTokenDays = setting.RefreshTokenDays,
        MaxFailedLoginAttempts = setting.MaxFailedLoginAttempts,
        LockoutMinutes = setting.LockoutMinutes,
        ReferralCodePrefix = setting.ReferralCodePrefix,
        ReferralCodeLength = setting.ReferralCodeLength,
        CommissionAttributionDays = setting.CommissionAttributionDays,
        MaxUploadSizeMb = setting.MaxUploadSizeMb,
        AllowedImageExtensions = setting.AllowedImageExtensions,
        AllowedDocumentExtensions = setting.AllowedDocumentExtensions,
        MaxImagesPerPost = setting.MaxImagesPerPost,
        RequirePostApproval = setting.RequirePostApproval,
        RequireGroupApproval = setting.RequireGroupApproval,
        AuditLogRetentionDays = setting.AuditLogRetentionDays,
        EnableEmailNotification = setting.EnableEmailNotification,
        NotificationSenderName = setting.NotificationSenderName,
        NotificationReplyTo = setting.NotificationReplyTo,
        Note = setting.Note,
        UpdatedAt = setting.UpdatedAt,
        UpdatedBy = setting.UpdatedBy
    };
}
