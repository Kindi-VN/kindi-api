using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Services;

/// <summary>
/// Mã chia sẻ riêng (refcode) — nguồn duy nhất là <c>Users.ReferralCode</c> cho MỌI loại tài khoản
/// (khách hàng, CTV, đối tác). Mã CTV trên hồ sơ (<c>Collaborators.CollaboratorCode</c>) là mã nghiệp vụ
/// riêng của hồ sơ, không dùng để chia sẻ — xem <see cref="IReferralService"/>.
/// </summary>
public class ReferralService : IReferralService
{
    private readonly IRepository<User> _userRepository;
    private readonly ISystemSettingService _systemSettingService;
    private readonly IRepository<ReferralEvent> _referralEventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IQueryService _queryService;

    public ReferralService(
        IRepository<User> userRepository,
        IRepository<ReferralEvent> referralEventRepository,
        ICurrentUserService currentUserService,
        IQueryService queryService,
        ISystemSettingService systemSettingService)
    {
        _userRepository = userRepository;
        _referralEventRepository = referralEventRepository;
        _currentUserService = currentUserService;
        _queryService = queryService;
        _systemSettingService = systemSettingService;
    }

    public async Task<string?> GetSharerReferralCodeAsync()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            return null;

        return await EnsureUserReferralCodeAsync(userId);
    }

    /// <summary>
    /// Mã chia sẻ của một tài khoản — nguồn DUY NHẤT là <c>Users.ReferralCode</c>: mọi loại tài khoản
    /// (khách hàng, CTV, đối tác, quản trị) dùng chung một loại mã, sinh ngay lần đầu cần tới.
    /// </summary>
    public async Task<string?> EnsureUserReferralCodeAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        // Đọc qua repository (context ghi, tracking) vì bên dưới Update chính entity này.
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.ReferralCode))
            return user.ReferralCode;

        user.ReferralCode = await GenerateUniqueCodeAsync();
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return user.ReferralCode;
    }

    public async Task<string?> ResolveAsync(string? referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return null;

        var code = referralCode.Trim().ToUpperInvariant();

        var exists = await _queryService.AnyAsync<User>(u => u.ReferralCode == code);

        return exists ? code : null;
    }

    public async Task<string?> ResolveForUserAsync(Guid userId, string? referralCode)
    {
        // Đọc qua repository (context ghi, tracking): bản ghi này được Update ngay bên dưới, còn đọc
        // no-tracking rồi Update sẽ lỗi khi request đã track cùng entity — luồng công khai vừa
        // tạo/lấy User (ResolvePublicUserAsync) rồi mới ghi nhận mã giới thiệu của link.
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return await ResolveAsync(referralCode);

        // Đã ghi nhận người giới thiệu → giữ nguyên, không đổi dù sau này mở link của CTV khác.
        if (!string.IsNullOrWhiteSpace(user.ReferredByCode))
            return user.ReferredByCode;

        var resolved = await ResolveAsync(referralCode);
        if (resolved == null)
            return null;

        user.ReferredByCode = resolved;
        user.ReferredAt = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        // Ghi nhận sự kiện "tài khoản được giới thiệu" (nền cho thống kê + hoa hồng sau này).
        await RecordEventAsync(resolved, userId, ReferralEventType.UserReferred, userId);

        return resolved;
    }

    public async Task<string?> AttributeToCurrentUserAsync(string? referralCode)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            return null;

        return await ResolveForUserAsync(userId, referralCode);
    }

    public async Task RecordEventAsync(
        string? referralCode,
        Guid referredUserId,
        ReferralEventType eventType,
        Guid? refEntityId = null,
        string? refEntityCode = null,
        decimal? amount = null,
        bool isGuestAccount = false)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return;

        var code = referralCode.Trim().ToUpperInvariant();

        // Bản ghi đã được ghi nhận trước đó (ví dụ huỷ rồi tham gia lại) → mở lại thay vì tạo trùng.
        if (refEntityId.HasValue)
        {
            var existing = await _referralEventRepository.GetFirstAsync(e =>
                e.EventType == eventType && e.RefEntityId == refEntityId.Value);

            if (existing != null)
            {
                existing.Status = ReferralEventStatus.Pending;
                existing.Amount = amount ?? existing.Amount;
                existing.UpdatedAt = DateTime.UtcNow;
                _referralEventRepository.Update(existing);
                await _referralEventRepository.SaveChangesAsync();
                return;
            }
        }

        // Chủ mã: mã chia sẻ chỉ nằm ở bảng Users (nguồn duy nhất).
        var referrerUserId = (await _queryService.GetFirstOrDefaultAsync<User>(u => u.ReferralCode == code))?.Id;

        // Mã của chính người phát sinh thì không ghi nhận.
        if (referrerUserId == referredUserId)
            return;

        await _referralEventRepository.AddAsync(new ReferralEvent
        {
            ReferralEventCode = CodeGenerator.Generate("RFE"),
            ReferralCode = code,
            ReferrerUserId = referrerUserId,
            ReferredUserId = referredUserId,
            EventType = eventType,
            RefEntityId = refEntityId,
            RefEntityCode = refEntityCode,
            Amount = amount,
            IsGuestAccount = isGuestAccount,
            Status = ReferralEventStatus.Pending
        });
        await _referralEventRepository.SaveChangesAsync();
    }

    public Task SetEventStatusAsync(ReferralEventType eventType, Guid refEntityId, ReferralEventStatus? status)
        => SetEventStatusAsync(eventType, new[] { refEntityId }, status);

    public async Task SetEventStatusAsync(ReferralEventType eventType, IEnumerable<Guid> refEntityIds, ReferralEventStatus? status)
    {
        if (status == null)
            return;

        var ids = refEntityIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        var events = (await _referralEventRepository.FindAsync(e =>
            e.EventType == eventType && e.RefEntityId != null && ids.Contains(e.RefEntityId.Value))).ToList();

        if (events.Count == 0)
            return;

        foreach (var referralEvent in events)
        {
            referralEvent.Status = status.Value;
            referralEvent.UpdatedAt = DateTime.UtcNow;
        }

        _referralEventRepository.UpdateRange(events);
        await _referralEventRepository.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string?> referralCodes)
    {
        var codes = referralCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return new Dictionary<string, string>();

        var users = await _queryService.GetListAsync<User>(
            u => u.ReferralCode != null && codes.Contains(u.ReferralCode));

        return users
            .Where(u => u.ReferralCode != null)
            .GroupBy(u => u.ReferralCode!)
            .ToDictionary(g => g.Key, g => g.First().FullName);
    }

    public async Task FillNamesAsync<T>(
        IEnumerable<T> items,
        Func<T, string?> getReferralCode,
        Action<T, string> setReferralName)
    {
        var list = items.ToList();
        if (list.Count == 0)
            return;

        var names = await LoadNamesAsync(list.Select(getReferralCode));
        if (names.Count == 0)
            return;

        foreach (var item in list)
        {
            var code = getReferralCode(item);
            if (code != null && names.TryGetValue(code, out var name))
                setReferralName(item, name);
        }
    }

    /// <summary>
    /// Mã chia sẻ sinh theo Cài đặt chung (tiền tố + độ dài, mặc định KND + 8 ký tự), không dấu phân cách,
    /// và không trùng mã chia sẻ của tài khoản nào khác.
    /// </summary>
    private async Task<string> GenerateUniqueCodeAsync()
    {
        var setting = await _systemSettingService.GetAsync();
        var prefix = string.IsNullOrWhiteSpace(setting.ReferralCodePrefix) ? "KND" : setting.ReferralCodePrefix;
        var length = setting.ReferralCodeLength > 0 ? setting.ReferralCodeLength : 8;

        string code;
        bool exists;
        do
        {
            code = CodeGenerator.GenerateCompact(prefix, length);
            exists = await _queryService.AnyAsync<User>(u => u.ReferralCode == code);
        } while (exists);

        return code;
    }
}
