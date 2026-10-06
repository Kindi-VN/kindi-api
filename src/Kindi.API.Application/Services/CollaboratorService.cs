using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.Errors;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Errors;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Expressions;
using Kindi.API.Shared.Resources;

namespace Kindi.API.Application.Services;

public class CollaboratorService : ICollaboratorService
{
    private readonly IRepository<Collaborator> _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IStringLocalizer<ExceptionMessages> _exceptionLocalizer;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<BusinessField> _businessFieldRepo;
    private readonly IBusinessFieldService _businessFieldService;
    private readonly ICompanyService _companyService;
    private readonly IReferralService _referralService;

    public CollaboratorService(
        IRepository<Collaborator> repository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService,
        IStringLocalizer<SharedResource> localizer,
        IRepository<User> userRepo,
        IStringLocalizer<ExceptionMessages> exceptionLocalizer,
        IRepository<BusinessField> businessFieldRepo,
        IBusinessFieldService businessFieldService,
        ICompanyService companyService,
        IReferralService referralService)
    {
        _businessFieldService = businessFieldService;
        _repository = repository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
        _localizer = localizer;
        _exceptionLocalizer = exceptionLocalizer;
        _userRepo = userRepo;
        _businessFieldRepo = businessFieldRepo;
        _companyService = companyService; // injected by DI (ICompanyService)
        _referralService = referralService;
    }

    public async Task<CollaboratorResponseDto> CreateAsync(CreateCollaboratorDto request)
    {
        // Lấy hoặc tạo User (dùng UserService)
        Guid userGuid;
        var userId = _currentUserService.UserId;

        // Đăng ký công khai (chưa đăng nhập): cần biết trước đây là tài khoản mới hay dùng lại
        // để trả thông tin đăng nhập (username user<sđt> / mật khẩu = SĐT) cho người đăng ký.
        var isPublicRegistration = string.IsNullOrEmpty(userId);
        var isNewAccount = false;
        string? accountUsername = null;

        if (!string.IsNullOrEmpty(userId))
        {
            userGuid = Guid.Parse(userId);
        }
        else
        {
            var existingUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
            // Tài khoản xoá mềm được khôi phục kèm đăng nhập mới → coi như tài khoản mới.
            isNewAccount = existingUser == null || existingUser.IsDeleted;

            // UserService kiểm tra phone/email, khôi phục tài khoản đã xoá mềm nếu có
            userGuid = await _userService.GetOrCreateUserAsync(
                request.FullName,
                request.Phone,
                request.Email
            );

            if (isNewAccount)
            {
                var createdUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
                accountUsername = createdUser?.Username;
            }
        }

        //  Một tài khoản (bảng Users) chỉ có tối đa một hồ sơ CTV — tìm cả bản ghi đã xoá mềm:
        //  hồ sơ đang hoạt động thì báo đã là CTV, hồ sơ đã xoá thì khôi phục lại bên dưới.
        var existingCollaborator = await _repository.GetFirstAsync(c => c.UserId == userGuid);

        if (existingCollaborator is { IsDeleted: false })
            throw new AppException(CollaboratorError.UserAlreadyExists);

        //  Thông tin cá nhân (họ tên/SĐT/email/Zalo) chỉ lưu ở bảng Users — ghi qua UserService;
        //  luồng công khai không được ghi đè hồ sơ của tài khoản đã tồn tại (chỉ điền chỗ trống).
        await _userService.UpdatePersonalInfoAsync(
            userGuid, request.FullName, request.Phone, request.Email, request.Zalo);

        //  Hồ sơ CTV đã xoá mềm của chính tài khoản này → khôi phục hồ sơ cũ (giữ mã CTV/mã giới thiệu)
        //  thay vì tạo bản ghi mới, tránh lỗi ràng buộc mỗi tài khoản chỉ một hồ sơ.
        var collaborator = existingCollaborator ?? _mapper.Map<Collaborator>(request);
        if (existingCollaborator != null)
            _mapper.Map(request, collaborator);

        collaborator.IsDeleted = false;
        collaborator.UserId = userGuid;
        collaborator.Status = CollaboratorStatus.Pending;
        collaborator.IsApproved = false;
        collaborator.Level = 1;

        if (string.IsNullOrWhiteSpace(collaborator.CollaboratorCode))
            collaborator.CollaboratorCode = await GenerateUniqueCollaboratorCodeAsync();
        // Mã chia sẻ riêng của CTV = mã CTV trên hồ sơ (dùng để gắn vào link chia sẻ).
        if (string.IsNullOrWhiteSpace(collaborator.ReferralCode))
            collaborator.ReferralCode = collaborator.CollaboratorCode;

        //  Xử lý BusinessField — ưu tiên Id (chọn từ danh sách quản lý tập trung),
        //  fallback sang find-or-create theo tên cho client chưa gửi Id.
        collaborator.BusinessFieldId = null;
        collaborator.BusinessFieldName = null;

        if (request.BusinessFieldId.HasValue)
        {
            var field = await _businessFieldRepo.GetFirstAsync(
                b => b.Id == request.BusinessFieldId.Value && !b.IsDeleted
            );

            if (field == null)
                throw new BadRequestException("Lĩnh vực hoạt động không tồn tại");

            collaborator.BusinessFieldId = field.Id;
            collaborator.BusinessFieldName = field.Name;
        }
        else if (!string.IsNullOrWhiteSpace(request.BusinessFieldName))
        {
            // Dùng chung BusinessFieldService để tránh lệch convention NormalizedName.
            collaborator.BusinessFieldId =
                await _businessFieldService.GetOrCreateBusinessFieldAsync(request.BusinessFieldName);
            collaborator.BusinessFieldName = request.BusinessFieldName.Trim();
        }

        //  Xử lý Parent
        if (request.ParentCollaboratorId.HasValue)
        {
            var parent = await _repository.GetFirstAsync(c =>
                c.Id == request.ParentCollaboratorId.Value && !c.IsDeleted);

            if (parent == null)
                throw new AppException(CollaboratorError.ParentNotFound.WithParams(request.ParentCollaboratorId.Value));

            if (!parent.IsApproved)
                throw new AppException(CollaboratorError.ParentNotApproved.WithParams(request.ParentCollaboratorId.Value));

            if (parent.Level >= 10)
                throw new AppException(CollaboratorError.LevelExceeded.WithParams(10));

            if (await IsCircularReferenceAsync(request.ParentCollaboratorId.Value, userGuid))
                throw new AppException(CollaboratorError.CircularReference.WithParams(request.ParentCollaboratorId.Value));

            collaborator.Level = parent.Level + 1;
        }

        //  Lưu
        // Ensure Company created/linked from collaborator registration
        var company = await _companyService.AddOrUpdateFromLegacyAsync(
            request.BusinessName, request.CompanyTax, request.Address, request.Website, request.BusinessFieldId,
            businessType: null, companySize: request.BusinessSize.HasValue ? (Kindi.API.Domain.Enums.CompanySize?)request.BusinessSize.Value : null);
        if (company != null)
        {
            collaborator.CompanyId = company.Id;
            // keep legacy BusinessName for compatibility
            collaborator.BusinessName = request.BusinessName;
            collaborator.BusinessSize = request.BusinessSize;
            collaborator.Website = request.Website;
        }

        if (existingCollaborator != null)
            _repository.Update(collaborator);
        else
            await _repository.AddAsync(collaborator);

        await _repository.SaveChangesAsync();

        //  Mã chia sẻ trên link (?ref=) → ghi nhận người giới thiệu cho tài khoản đăng ký (chỉ ghi lần đầu,
        //  mã không nhận diện được thì bỏ qua, không chặn đăng ký). Mã chia sẻ của chính CTV vẫn là mã CTV.
        await _referralService.ResolveForUserAsync(collaborator.UserId, request.AccountReferrerCode);

        // Nạp tài khoản vào navigation để response trả họ tên/SĐT/email/Zalo (dữ liệu ở bảng Users).
        collaborator.User = await _userService.FindByIdAsync(collaborator.UserId) ?? collaborator.User;

        var response = _mapper.Map<CollaboratorResponseDto>(collaborator);
        await _referralService.FillNamesAsync(new[] { response }, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);

        if (isPublicRegistration)
        {
            response.Account = new AccountCredentialsDto
            {
                IsNewAccount = isNewAccount,
                AccountAlreadyExisted = !isNewAccount,
                Username = accountUsername,
                PasswordIsPhone = isNewAccount
            };
        }

        return response;
    }

    /// <summary>
    /// Tạo hồ sơ cộng tác viên (chờ duyệt) cho người dùng nếu chưa có. Dùng cho các luồng công khai
    /// (mua chung, nhóm ngành) — nơi DUY NHẤT sinh bản ghi ở bảng <c>Collaborators</c>.
    /// </summary>
    public async Task EnsureProfileForUserAsync(Guid userId)
    {
        // Mỗi tài khoản chỉ một hồ sơ CTV: đang hoạt động thì thôi, đã xoá mềm thì khôi phục lại.
        var existing = await _repository.GetFirstAsync(c => c.UserId == userId);
        if (existing != null)
        {
            if (!existing.IsDeleted)
                return;

            existing.IsDeleted = false;
            existing.Status = CollaboratorStatus.Pending;
            existing.IsApproved = false;
            if (string.IsNullOrWhiteSpace(existing.ReferralCode))
                existing.ReferralCode = existing.CollaboratorCode;

            _repository.Update(existing);
            await _repository.SaveChangesAsync();
            return;
        }

        var collaborator = new Collaborator
        {
            UserId = userId,
            CollaboratorCode = await GenerateUniqueCollaboratorCodeAsync(),
            Status = CollaboratorStatus.Pending,
            IsApproved = false,
            Level = 1
        };
        // Mã chia sẻ riêng của CTV = mã CTV trên hồ sơ (dùng để gắn vào link chia sẻ).
        collaborator.ReferralCode = collaborator.CollaboratorCode;

        await _repository.AddAsync(collaborator);
        await _repository.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueCollaboratorCodeAsync()
    {
        string code;
        bool exists;
        do
        {
            code = CodeGenerator.Generate("CTV");
            exists = await _repository.AnyAsync(c => c.CollaboratorCode == code);
        } while (exists);
        return code;
    }

    private async Task<bool> IsCircularReferenceAsync(Guid parentId, Guid userId)
    {
        var currentId = parentId;
        var visitedIds = new HashSet<Guid>();

        while (currentId != Guid.Empty)
        {
            if (visitedIds.Contains(currentId))
                return true;

            visitedIds.Add(currentId);

            var parent = await _repository.GetFirstAsync(c => c.Id == currentId && !c.IsDeleted);
            if (parent == null || parent.UserId == userId)
                return parent?.UserId == userId;

            currentId = parent.ParentCollaboratorId ?? Guid.Empty;
        }
        return false;
    }

    public async Task<CollaboratorResponseDto> UpdateAsync(Guid id, UpdateCollaboratorDto request)
    {
        var collaborator = await _repository.GetByIdAsync(id);
        if (collaborator == null)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        // Partial update: field nào null thì AutoMapper giữ nguyên giá trị cũ
        // (BusinessFieldId/BusinessFieldName đã Ignore, xử lý tay bên dưới).
        _mapper.Map(request, collaborator);

        // Lĩnh vực kinh doanh — cùng logic với CreateAsync: ưu tiên Id, fallback find-or-create
        // theo tên, và luôn ghi lại BusinessFieldName để cột denormalized khớp với Id.
        if (request.BusinessFieldId.HasValue)
        {
            var field = await _businessFieldRepo.GetFirstAsync(
                b => b.Id == request.BusinessFieldId.Value && !b.IsDeleted);

            if (field == null)
                throw new BadRequestException("Lĩnh vực hoạt động không tồn tại");

            collaborator.BusinessFieldId = field.Id;
            collaborator.BusinessFieldName = field.Name;
        }
        else if (!string.IsNullOrWhiteSpace(request.BusinessFieldName))
        {
            collaborator.BusinessFieldId =
                await _businessFieldService.GetOrCreateBusinessFieldAsync(request.BusinessFieldName);
            collaborator.BusinessFieldName = request.BusinessFieldName.Trim();
        }

        // Company: if collaborator provided business/company info, create or update Company and link
        // Use collaborator's denormalized fields (they were updated by AutoMapper when non-null)
        if (!string.IsNullOrWhiteSpace(collaborator.BusinessName) || !string.IsNullOrWhiteSpace(collaborator.Website) || collaborator.BusinessSize.HasValue)
        {
            var company = await _companyService.AddOrUpdateFromLegacyAsync(
                collaborator.BusinessName,
                null,
                collaborator.Address,
                collaborator.Website,
                collaborator.BusinessFieldId,
                businessType: null,
                companySize: collaborator.BusinessSize.HasValue ? (Kindi.API.Domain.Enums.CompanySize?)collaborator.BusinessSize.Value : null);

            if (company != null)
            {
                collaborator.CompanyId = company.Id;
            }
        }

        _repository.Update(collaborator);
        await _repository.SaveChangesAsync();

        // Thông tin cá nhân chỉ lưu ở bảng Users; endpoint admin này được phép ghi đè SĐT/email.
        await _userService.UpdatePersonalInfoAsync(
            collaborator.UserId, request.FullName, request.Phone, request.Email, request.Zalo,
            allowContactChange: true);

        // Nạp lại navigation (User/BusinessField/Company) để response trả đủ dữ liệu sau cập nhật.
        var updated = await _repository.GetFirstWithIncludesAsync(
            c => c.Id == id,
            q => q.IncludeMultiple(c => c.User, c => c.BusinessField, c => c.Company));

        var dto = _mapper.Map<CollaboratorResponseDto>(updated ?? collaborator);
        await _referralService.FillNamesAsync(new[] { dto }, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);
        return dto;
    }

    public async Task<CollaboratorResponseDto> GetByIdAsync(Guid id)
    {
        var collaborator = await _repository.GetFirstWithIncludesAsync(
            c => c.Id == id,
            q => q.IncludeMultiple(c => c.User, c => c.BusinessField, c => c.Company));

        if (collaborator == null)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        var dto = _mapper.Map<CollaboratorResponseDto>(collaborator);
        await _referralService.FillNamesAsync(new[] { dto }, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);
        return dto;
    }

    public async Task<PagedList<CollaboratorResponseDto>> GetPagedAsync(
        int page,
        int size,
        string? search = null,
        CollaboratorStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CollaboratorSearchField? searchField = null)
    {
        // Điều kiện trạng thái + khoảng ngày tạo — áp dụng cho mọi nhánh truy vấn.
        Expression<Func<Collaborator, bool>> predicate = c =>
            (!status.HasValue || c.Status == status.Value)
            && (!fromDate.HasValue || c.CreatedAt >= fromDate.Value.Date.ToUniversalTime())
            && (!toDate.HasValue || c.CreatedAt < toDate.Value.Date.AddDays(1).ToUniversalTime());

        if (!string.IsNullOrEmpty(search))
        {
            // Từ khoá đã trim + escape; mẫu LIKE được ghép ngay trong biểu thức truy vấn,
            // ILIKE nên tìm không phân biệt hoa/thường.
            var searchTerm = search.RemoveVietnameseSign().ToLikeEscaped();

            // searchField chỉ định thì CHỈ dò đúng một cột; bỏ trống giữ nguyên hành vi cũ (dò nhiều trường).
            Expression<Func<Collaborator, bool>> searchPredicate = searchField switch
            {
                CollaboratorSearchField.FullName => c => c.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.FullName), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.CollaboratorCode => c => c.CollaboratorCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.CollaboratorCode), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.UserCode => c => c.User != null && c.User.UserCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.UserCode), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.ReferralCode => c => c.ReferralCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.ReferralCode), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.AccountReferrerCode => c => c.User != null && c.User.AccountReferrerCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.AccountReferrerCode), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.Phone => c => c.User != null && c.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.Phone), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.Email => c => c.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.Email), "%" + searchTerm + "%", "\\"),
                CollaboratorSearchField.BusinessFieldName => c =>
                    (c.BusinessFieldName != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.BusinessFieldName), "%" + searchTerm + "%", "\\")) ||
                    (c.BusinessField != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.BusinessField.Name), "%" + searchTerm + "%", "\\")),
                _ => c => (c.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.FullName), "%" + searchTerm + "%", "\\") ||
                             (c.User != null && c.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.Phone), "%" + searchTerm + "%", "\\")) ||
                             (c.User != null && c.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.Email), "%" + searchTerm + "%", "\\")) ||
                             (c.CollaboratorCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.CollaboratorCode), "%" + searchTerm + "%", "\\")) ||
                             // Tìm theo lĩnh vực kinh doanh: khớp cả cột denormalized
                             // (bản ghi cũ) lẫn tên trong bảng BusinessFields (tên hiển thị).
                             (c.BusinessFieldName != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.BusinessFieldName), "%" + searchTerm + "%", "\\")) ||
                             (c.BusinessField != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.BusinessField.Name), "%" + searchTerm + "%", "\\")) ||
                             (c.BusinessField != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.BusinessField.NormalizedName), "%" + searchTerm + "%", "\\")))
            };

            predicate = predicate.And(searchPredicate);
        }

        var paged = await _repository.GetPagedWithIncludesAsync(
            page, size,
            includes: q => q.IncludeMultiple(c => c.User, c => c.BusinessField),
            predicate: predicate,
            orderBy: c => c.CreatedAt,
            isDescending: true);

        // Temporary migration for paged collaborators: ensure Company created/linked.
        foreach (var item in paged.Items)
        {
            if (!item.CompanyId.HasValue && !string.IsNullOrWhiteSpace(item.BusinessName))
            {
                var company = await _companyService.AddOrUpdateFromLegacyAsync(
                    item.BusinessName, null, item.Address, item.Website, item.BusinessFieldId);
                if (company != null)
                {
                    // Persist CompanyId back to the tracked Collaborator entity
                    var tracked = await _repository.GetByIdAsync(item.Id);
                    if (tracked != null)
                    {
                        tracked.CompanyId = company.Id;
                        _repository.Update(tracked);
                        await _repository.SaveChangesAsync();
                        item.CompanyId = company.Id; // update in-memory item for response
                    }
                }
            }
        }

        var items = _mapper.Map<List<CollaboratorResponseDto>>(paged.Items);
        await _referralService.FillNamesAsync(items, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);

        return new PagedList<CollaboratorResponseDto>(
            items,
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }

    public async Task<PagedList<CollaboratorResponseDto>> GetPagedDeletedAsync(int page, int size, string? search = null)
    {
        var query = _repository.GetQueryable().IgnoreQueryFilters().Where(c => c.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            query = query.Where(c =>
                (c.User != null && c.User.FullName.Contains(s)) ||
                (c.User != null && c.User.Phone != null && c.User.Phone.Contains(s)) ||
                (c.User != null && c.User.Email != null && c.User.Email.Contains(s)) ||
                (c.CollaboratorCode != null && c.CollaboratorCode.Contains(s)) ||
                (c.BusinessFieldName != null && c.BusinessFieldName.Contains(s)) ||
                (c.BusinessField != null && c.BusinessField.Name.Contains(s)));
        }

        query = query.IncludeMultiple(c => c.User, c => c.BusinessField, c => c.Company).OrderByDescending(c => c.CreatedAt);

        var paged = await PagedList<Collaborator>.CreateAsync(query, page, size);

        var items = _mapper.Map<List<CollaboratorResponseDto>>(paged.Items);
        await _referralService.FillNamesAsync(items, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);

        return new PagedList<CollaboratorResponseDto>(
            items,
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }

    public async Task ApproveAsync(Guid id)
    {
        var collaborator = await _repository.GetByIdAsync(id);
        if (collaborator == null)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        collaborator.Status = CollaboratorStatus.Approved;
        collaborator.IsApproved = true;
        collaborator.ApprovedAt = DateTime.UtcNow;

        _repository.Update(collaborator);
        await _repository.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid id, string? reason = null)
    {
        var collaborator = await _repository.GetByIdAsync(id);
        if (collaborator == null)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        collaborator.Status = CollaboratorStatus.Rejected;
        collaborator.IsApproved = false;
        collaborator.RejectedAt = DateTime.UtcNow;
        collaborator.RejectionReason = reason;

        _repository.Update(collaborator);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var collaborator = await _repository.GetByIdAsync(id);
        if (collaborator == null)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        _repository.Delete(collaborator);
        await _repository.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid id)
    {
        // GetByIdIncludingDeletedAsync bỏ qua global soft-delete filter → tìm được record đã xóa mềm.
        var collaborator = await _repository.GetByIdIncludingDeletedAsync(id);
        if (collaborator == null || !collaborator.IsDeleted)
            throw new AppException(CollaboratorError.NotFound.WithParams(id));

        _repository.Restore(collaborator);
        await _repository.SaveChangesAsync();
    }
}