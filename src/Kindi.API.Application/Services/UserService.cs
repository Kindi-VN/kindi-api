using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Models;
using Kindi.API.Application.Errors;
using Kindi.API.Shared.Errors;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Microsoft.EntityFrameworkCore;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IStringLocalizer<ExceptionMessages> _exceptionLocalizer;
    private readonly IAuthAuditService _authAuditService;

    public UserService(
        IRepository<User> userRepo,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        IStringLocalizer<ExceptionMessages> exceptionLocalizer,
        IAuthAuditService authAuditService)
    {
        _userRepo = userRepo;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _exceptionLocalizer = exceptionLocalizer;
        _authAuditService = authAuditService;
    }

    /// <summary>
    /// Lấy tài khoản theo SĐT/email, chưa có thì tạo mới. Tài khoản đã xoá mềm được khôi phục lại
    /// chính bản ghi cũ (giữ lịch sử đơn hàng/audit). Mọi tài khoản sinh từ đây đăng nhập bằng
    /// SĐT làm mật khẩu và buộc đổi tên đăng nhập + mật khẩu ở lần đăng nhập đầu.
    /// </summary>
    public async Task<Guid> GetOrCreateUserAsync(string fullName, string phone, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw UserException.PhoneRequired(_exceptionLocalizer);

        var existing = await FindByPhoneOrEmailAsync(phone, email);

        if (existing != null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.IsActive = true;
                existing.FullName = fullName.Trim();
                if (!string.IsNullOrWhiteSpace(email))
                    existing.Email = email.Trim();
                existing.PasswordHash = HashPassword(existing.Phone ?? phone.Trim());
                existing.MustChangeCredentials = true;

                _userRepo.Update(existing);
                await _userRepo.SaveChangesAsync();
                await _authAuditService.LogAsync(existing.Id, existing.Username, AuditAction.Register, true,
                    $"Khôi phục tài khoản đã xoá (SĐT: {existing.Phone})");
                return existing.Id;
            }

            existing.FullName = fullName;
            if (!string.IsNullOrWhiteSpace(email))
                existing.Email = email.Trim();

            _userRepo.Update(existing);
            await _userRepo.SaveChangesAsync();
            return existing.Id;
        }

        var user = new User
        {
            UserCode = CodeGenerator.Generate("USR"),
            FullName = fullName.Trim(),
            Phone = phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{phone.Trim()}@temp.com" : email.Trim(),
            Username = GenerateUniqueUsername(phone),
            PasswordHash = HashPassword(phone.Trim()),
            Role = UserRole.User,
            IsActive = true,
            MustChangeCredentials = true
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();
        await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.Register, true,
            $"Tạo tài khoản mới (SĐT: {phone})");
        return user.Id;
    }

    public async Task<User?> FindByIdAsync(Guid userId)
        => await _userRepo.GetFirstAsync(u => u.Id == userId && !u.IsDeleted);

    public async Task<User?> FindByPhoneOrEmailAsync(string? phone, string? email)
    {
        // SĐT tra theo cả dạng đã chuẩn hoá (bỏ khoảng trắng/dấu, +84 → 0); email đối chiếu tuyệt đối.
        var phoneCandidates = PhoneHelper.Candidates(phone);
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (phoneCandidates.Count == 0 && normalizedEmail == null)
            return null;

        return await _userRepo.GetFirstAsync(u =>
            phoneCandidates.Contains(u.Phone ?? string.Empty) ||
            (normalizedEmail != null && u.Email == normalizedEmail));
    }

    public async Task<PublicUserResult> ResolvePublicUserAsync(string fullName, string phone, string? email, string? zalo)
    {
        var existing = await FindByPhoneOrEmailAsync(phone, email);
        // Tài khoản xoá mềm cũng được khôi phục kèm đăng nhập mới → coi như tài khoản mới.
        var isNewAccount = existing == null || existing.IsDeleted;

        // Tài khoản đã có: dùng lại, KHÔNG ghi đè hồ sơ (đây là endpoint công khai).
        var userId = existing?.Id ?? await GetOrCreateUserAsync(fullName.Trim(), phone.Trim(), email);

        await UpdatePersonalInfoAsync(userId, fullName, phone, email, zalo);

        return new PublicUserResult(userId, isNewAccount);
    }

    public async Task<UserPersonalInfo?> GetPersonalInfoAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return null;

        return new UserPersonalInfo(user.FullName, user.Phone, UserInfo.DisplayEmail(user.Email, user.Phone), user.Zalo);
    }

    public async Task UpdatePersonalInfoAsync(Guid userId, string? fullName, string? phone, string? email, string? zalo, bool allowContactChange = false)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return;

        var changed = false;

        var newFullName = fullName?.Trim();
        if (!string.IsNullOrWhiteSpace(newFullName) && !string.Equals(user.FullName, newFullName, StringComparison.Ordinal))
        {
            user.FullName = newFullName;
            changed = true;
        }

        var newZalo = zalo?.Trim();
        if (!string.IsNullOrWhiteSpace(newZalo) && !string.Equals(user.Zalo, newZalo, StringComparison.Ordinal))
        {
            user.Zalo = newZalo;
            changed = true;
        }

        // SĐT/email: chỉ ghi khi luồng cho phép (admin) hoặc tài khoản đang trống.
        var newPhone = phone?.Trim();
        if (!string.IsNullOrWhiteSpace(newPhone)
            && !string.Equals(user.Phone, newPhone, StringComparison.Ordinal)
            && (allowContactChange || string.IsNullOrWhiteSpace(user.Phone)))
        {
            if (_userRepo.GetQueryable().Any(u => u.Id != userId && u.Phone == newPhone))
                throw new AppException(UserError.PhoneAlreadyExists.WithParams(newPhone));

            user.Phone = newPhone;
            changed = true;
        }

        var newEmail = email?.Trim();
        if (!string.IsNullOrWhiteSpace(newEmail)
            && !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase)
            && (allowContactChange || UserInfo.IsPlaceholderEmail(user.Email, user.Phone)))
        {
            if (_userRepo.GetQueryable().Any(u => u.Id != userId && u.Email == newEmail))
                throw new AppException(UserError.EmailAlreadyExists.WithParams(newEmail));

            user.Email = newEmail;
            changed = true;
        }

        if (!changed)
            return;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _userRepo.GetByIdAsync(Guid.Parse(userId));
    }

    private string GenerateUniqueUsername(string phone)
    {
        // Username gắn với SĐT cho dễ đọc (vd: user0912345678); fallback random khi không có SĐT.
        var normalized = PhoneHelper.Normalize(phone);
        var baseUsername = string.IsNullOrEmpty(normalized)
            ? $"user{Guid.NewGuid():N}"[..50]
            : $"user{normalized}";

        // Đảm bảo không trùng username đã có.
        var exists = _userRepo.GetQueryable().Any(u => u.Username == baseUsername);
        if (!exists) return baseUsername;

        return $"{baseUsername}_{Guid.NewGuid():N}"[..50];
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Danh sách người dùng phân trang cho màn quản trị — tìm không phân biệt hoa/thường
    /// theo tên đăng nhập, họ tên, số điện thoại, email hoặc mã người dùng.
    /// </summary>
    public async Task<PagedList<UserInfoResponse>> GetPagedAsync(UserQueryDto query)
    {
        // Tài khoản SuperAdmin không hiện ở màn quản trị của admin thường.
        var users = _userRepo.GetQueryable().Where(u => !u.IsDeleted && u.Role != UserRole.SuperAdmin);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn, ILIKE nên tìm không phân biệt hoa/thường.
            var searchTerm = query.Search.RemoveVietnameseSign().ToLikeEscaped();
            users = users.Where(u => EF.Functions.ILike(KindiDbFunctions.Unaccent(u.FullName), "%" + searchTerm + "%", "\\") ||
                                     (u.Username != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(u.Username), "%" + searchTerm + "%", "\\")) ||
                                     (u.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(u.Phone), "%" + searchTerm + "%", "\\")) ||
                                     (u.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(u.Email), "%" + searchTerm + "%", "\\")) ||
                                     (u.UserCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(u.UserCode), "%" + searchTerm + "%", "\\")));
        }

        var ordered = users.OrderByDescending(u => u.CreatedAt);
        return await PagedList<UserInfoResponse>.CreateAsync(ordered.Select(u => ToInfoResponse(u)), query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Cấp lại mật khẩu về số điện thoại của tài khoản và bắt buộc đổi ở lần đăng nhập kế tiếp.
    /// Tài khoản quản trị không cấp lại theo cách này.
    /// </summary>
    public async Task<UserInfoResponse?> ResetPasswordToPhoneAsync(Guid userId)
    {
        var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
            return null;

        if (user.Role is UserRole.Admin or UserRole.SuperAdmin)
            throw UserException.AdminResetNotAllowed(_exceptionLocalizer);

        var phone = PhoneHelper.Normalize(user.Phone);

        if (string.IsNullOrEmpty(phone))
            throw UserException.PhoneRequired(_exceptionLocalizer);

        user.PasswordHash = PasswordHasher.Hash(phone);
        user.MustChangeCredentials = true;
        await _userRepo.SaveChangesAsync();

        await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.ResetPassword, true,
            "Quản trị cấp lại mật khẩu bằng số điện thoại");

        return ToInfoResponse(user);
    }

    /// <summary>Thông tin tài khoản trả ra DTO (email tạm <c>{sđt}@temp.com</c> không trả ra ngoài).</summary>
    private static UserInfoResponse ToInfoResponse(User user) => new()
    {
        Id = user.Id,
        UserCode = user.UserCode,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email != null && user.Email.EndsWith("@temp.com", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : user.Email,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        MustChangeCredentials = user.MustChangeCredentials,
        LastLoginAt = user.LastLoginAt
    };
}