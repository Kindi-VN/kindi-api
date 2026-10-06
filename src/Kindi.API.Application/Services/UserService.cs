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
using Kindi.API.Domain.Rules;
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
    private readonly IRepository<Collaborator> _collaboratorRepo;
    private readonly IRepository<Partner> _partnerRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IStringLocalizer<ExceptionMessages> _exceptionLocalizer;
    private readonly IAuthAuditService _authAuditService;

    public UserService(
        IRepository<User> userRepo,
        IRepository<Collaborator> collaboratorRepo,
        IRepository<Partner> partnerRepo,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        IStringLocalizer<ExceptionMessages> exceptionLocalizer,
        IAuthAuditService authAuditService)
    {
        _userRepo = userRepo;
        _collaboratorRepo = collaboratorRepo;
        _partnerRepo = partnerRepo;
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

        // Họ tên: luồng công khai chỉ ĐIỀN CHỖ TRỐNG — hồ sơ đã có tên thì không ghi đè, tránh form
        // ghi tên rác lên tài khoản đã có (kể cả khi người gửi đang đăng nhập). Muốn đổi hồ sơ phải
        // qua endpoint của chính chủ hồ sơ hoặc quản trị (allowContactChange: true).
        var newFullName = fullName?.Trim();
        if (!string.IsNullOrWhiteSpace(newFullName)
            && (allowContactChange || string.IsNullOrWhiteSpace(user.FullName))
            && !string.Equals(user.FullName, newFullName, StringComparison.Ordinal))
        {
            user.FullName = newFullName;
            changed = true;
        }

        // Zalo: như họ tên — luồng công khai chỉ điền khi tài khoản chưa có Zalo.
        var newZalo = zalo?.Trim();
        if (!string.IsNullOrWhiteSpace(newZalo)
            && (allowContactChange || string.IsNullOrWhiteSpace(user.Zalo))
            && !string.Equals(user.Zalo, newZalo, StringComparison.Ordinal))
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

        // Tab "Tài khoản thường" / "Tài khoản quản trị" trên màn quản lý người dùng.
        users = query.Scope switch
        {
            UserAccountScope.Admin => users.Where(u => u.Role == UserRole.Admin),
            UserAccountScope.Customer => users.Where(u => u.Role != UserRole.Admin),
            _ => users
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Từ khoá chỉ cần trim: Like() bỏ dấu tiếng Việt và không phân biệt hoa/thường ở tầng DB.
            var searchTerm = query.Search.Trim();

            // searchField chỉ định thì CHỈ dò đúng một cột; bỏ trống giữ nguyên hành vi cũ (dò nhiều trường).
            users = query.SearchField switch
            {
                UserSearchField.Username => users.Where(u => u.Username.Like(searchTerm)),
                UserSearchField.FullName => users.Where(u => u.FullName.Like(searchTerm)),
                UserSearchField.UserCode => users.Where(u => u.UserCode != null && u.UserCode.EqualsCode(searchTerm)),
                UserSearchField.ReferralCode => users.Where(u => u.ReferralCode != null && u.ReferralCode.EqualsCode(searchTerm)),
                UserSearchField.AccountReferrerCode => users.Where(u => u.AccountReferrerCode != null && u.AccountReferrerCode.EqualsCode(searchTerm)),
                UserSearchField.Phone => users.Where(u => u.Phone != null && u.Phone.Like(searchTerm)),
                UserSearchField.Email => users.Where(u => u.Email.Like(searchTerm)),
                _ => users.Where(u => u.FullName.Like(searchTerm) ||
                                     (u.Username != null && u.Username.Like(searchTerm)) ||
                                     (u.Phone != null && u.Phone.Like(searchTerm)) ||
                                     (u.Email != null && u.Email.Like(searchTerm)) ||
                                     (u.UserCode != null && u.UserCode.EqualsCode(searchTerm)))
            };
        }

        var ordered = users.OrderByDescending(u => u.CreatedAt);
        return await PagedList<UserInfoResponse>.CreateAsync(ordered.Select(u => ToInfoResponse(u)), query.PageNumber, query.PageSize);
    }

    /// <summary>
    /// Tài khoản quản trị (Admin/SuperAdmin) không được gắn vào bất cứ dạng tài khoản nào: không nhận
    /// ghi nhận giới thiệu, không đứng tên hồ sơ CTV/đối tác.
    /// </summary>
    public async Task<bool> IsAdminAccountAsync(string? userId)
    {
        if (!Guid.TryParse(userId, out var id))
            return false;

        var user = await _userRepo.GetFirstAsync(u => u.Id == id && !u.IsDeleted
            && (u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin));

        return user != null;
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

    /// <summary>Số ký tự tối thiểu của mật khẩu đặt tay cho tài khoản quản trị.</summary>
    private const int MinPasswordLength = 8;

    /// <summary>
    /// Chi tiết tài khoản cho màn quản lý người dùng: thông tin tài khoản + hồ sơ CTV/đối tác liên kết.
    /// Tài khoản SuperAdmin không hiện với quản trị thường (giống màn danh sách).
    /// </summary>
    public async Task<UserDetailResponse?> GetDetailAsync(Guid userId)
    {
        var user = await FindManagedUserAsync(userId);
        if (user == null)
            return null;

        var collaborator = await _collaboratorRepo.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

        var partner = await _partnerRepo.GetQueryable()
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

        return new UserDetailResponse
        {
            User = ToInfoResponse(user),
            Collaborator = collaborator == null ? null : new UserCollaboratorSummaryDto
            {
                Id = collaborator.Id,
                CollaboratorCode = collaborator.CollaboratorCode,
                ReferralCode = collaborator.ReferralCode,
                Position = collaborator.Position,
                BusinessName = collaborator.BusinessName,
                BusinessFieldName = collaborator.BusinessFieldName,
                Website = collaborator.Website,
                Address = collaborator.Address,
                IsApproved = collaborator.IsApproved,
                Level = collaborator.Level,
                ApprovedAt = collaborator.ApprovedAt,
                RejectedAt = collaborator.RejectedAt
            },
            Partner = partner == null ? null : new UserPartnerSummaryDto
            {
                Id = partner.Id,
                PartnerCode = partner.PartnerCode,
                Position = partner.Position,
                CompanyName = partner.CompanyName,
                CompanyTax = partner.CompanyTax,
                CompanyAddress = partner.CompanyAddress,
                CompanyWebsite = partner.CompanyWebsite,
                Status = partner.Status.ToString(),
                ApprovedAt = partner.ApprovedAt
            }
        };
    }

    /// <summary>
    /// Sửa thông tin tài khoản ở màn quản lý — điểm ghi tập trung, được phép ghi đè hồ sơ
    /// (<c>allowContactChange: true</c>) vì đây là thao tác có chủ đích của quản trị.
    /// </summary>
    public async Task<UserInfoResponse?> UpdateInfoAsync(Guid userId, UpdateUserInfoRequest request)
    {
        var user = await FindManagedUserAsync(userId);
        if (user == null)
            return null;

        await UpdatePersonalInfoAsync(userId, request.FullName, request.Phone, request.Email, request.Zalo,
            allowContactChange: true);

        if (request.IsActive.HasValue && user.IsActive != request.IsActive.Value)
        {
            user.IsActive = request.IsActive.Value;
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        return ToInfoResponse(user);
    }

    /// <summary>
    /// Tạo tài khoản quản trị từ màn quản lý người dùng: mật khẩu do quản trị đặt tay nên KHÔNG bắt
    /// đổi ở lần đăng nhập đầu (<c>MustChangeCredentials = false</c>). Chặn trùng tên đăng nhập/email/SĐT
    /// trên cả bản ghi đã xoá mềm vì unique index của bảng Users không lọc IsDeleted.
    /// </summary>
    public async Task<UserInfoResponse> CreateAdminAsync(CreateAdminUserRequest request)
    {
        var username = (request.Username ?? string.Empty).Trim();
        var fullName = (request.FullName ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var password = (request.Password ?? string.Empty).Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : PhoneHelper.Normalize(request.Phone);

        if (string.IsNullOrWhiteSpace(username))
            throw new AppException(UserError.UsernameAlreadyExists);
        if (string.IsNullOrWhiteSpace(fullName))
            throw new AppException(UserError.FullNameRequired);
        if (string.IsNullOrWhiteSpace(email))
            throw new AppException(UserError.EmailRequired);
        if (password.Length < MinPasswordLength)
            throw new AppException(UserError.PasswordTooWeak.WithParams(MinPasswordLength));

        var existing = _userRepo.GetQueryable().IgnoreQueryFilters();
        if (await existing.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            throw new AppException(UserError.UsernameAlreadyExists.WithParams(username));
        if (await existing.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            throw new AppException(UserError.EmailAlreadyExists.WithParams(email));
        if (phone != null && await existing.AnyAsync(u => u.Phone == phone))
            throw new AppException(UserError.PhoneAlreadyExists.WithParams(phone));

        var user = new User
        {
            UserCode = CodeGenerator.Generate("USR"),
            ReferralCode = CodeGenerator.Generate("CTV"),
            Username = username,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Role = UserRole.Admin,
            IsActive = true,
            MustChangeCredentials = false,
            PasswordHash = HashPassword(password)
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        await _authAuditService.LogAsync(user.Id, user.Username, AuditAction.Register, true,
            $"Quản trị tạo tài khoản quản trị ({username})");

        return ToInfoResponse(user);
    }

    /// <summary>Gán / đổi vai trò tài khoản; không gán được SuperAdmin và không đụng tài khoản SuperAdmin.</summary>
    public async Task<UserInfoResponse?> UpdateRoleAsync(Guid userId, UserRole role)
    {
        if (!RoleRules.IsAssignable(role))
            throw new AppException(UserError.RoleNotAssignable);

        var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            return null;

        if (user.Role == UserRole.SuperAdmin)
            throw new AppException(UserError.SuperAdminImmutable);

        if (user.Role != role)
        {
            user.Role = role;
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        return ToInfoResponse(user);
    }

    /// <summary>
    /// Tài khoản mà màn quản lý người dùng được xem/sửa: bỏ tài khoản đã xoá mềm và tài khoản
    /// SuperAdmin (quản trị thường không thấy tài khoản này ở danh sách).
    /// </summary>
    private async Task<User?> FindManagedUserAsync(Guid userId)
    {
        var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
            return null;

        var isSuperAdminCaller = _currentUserService.IsInRole(UserRole.SuperAdmin);
        if (user.Role == UserRole.SuperAdmin && !isSuperAdminCaller)
            return null;

        return user;
    }

    /// <summary>Thông tin tài khoản trả ra DTO (email tạm <c>{sđt}@temp.com</c> không trả ra ngoài).</summary>
    private static UserInfoResponse ToInfoResponse(User user) => new()
    {
        Id = user.Id,
        ReferralCode = user.ReferralCode,
        AccountReferrerCode = user.AccountReferrerCode,
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