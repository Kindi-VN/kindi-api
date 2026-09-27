using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
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
        ICompanyService companyService)
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
            isNewAccount = existingUser == null;

            // UserService sẽ tự kiểm tra phone/email và throw exception nếu trùng
            userGuid = await _userService.GetOrCreateUserWithPhonePasswordAsync(
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

        //  SĐT/email đã đăng ký CTV trước đó → báo rõ ràng (2 cột này có unique index,
        //  để DB ném lỗi sẽ thành 500 khó hiểu cho người đăng ký).
        var existingCollaborator = await _repository.GetFirstAsync(c =>
            !c.IsDeleted &&
            (c.Phone == request.Phone ||
             (!string.IsNullOrEmpty(request.Email) && c.Email == request.Email)));

        if (existingCollaborator != null)
        {
            var isPhoneDuplicate = existingCollaborator.Phone == request.Phone;
            throw new BadRequestException(isPhoneDuplicate
                ? _localizer["Collaborator_PhoneAlreadyExists"]
                : _localizer["Collaborator_EmailAlreadyExists"]);
        }

        //  Tạo Collaborator
        var collaborator = _mapper.Map<Collaborator>(request);
        collaborator.UserId = userGuid;
        collaborator.CollaboratorCode = await GenerateUniqueCollaboratorCodeAsync();
        collaborator.Status = CollaboratorStatus.Pending;
        collaborator.IsApproved = false;
        collaborator.Level = 1;
        // Mã chia sẻ riêng của CTV = mã CTV trên hồ sơ (dùng để gắn vào link chia sẻ).
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
                throw CollaboratorException.ParentNotFound(_exceptionLocalizer, request.ParentCollaboratorId.Value);

            if (!parent.IsApproved)
                throw CollaboratorException.ParentNotApproved(_exceptionLocalizer, request.ParentCollaboratorId.Value);

            if (parent.Level >= 10)
                throw CollaboratorException.LevelExceeded(_exceptionLocalizer, 10);

            if (await IsCircularReferenceAsync(request.ParentCollaboratorId.Value, userGuid))
                throw CollaboratorException.CircularReference(_exceptionLocalizer, request.ParentCollaboratorId.Value);

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

        await _repository.AddAsync(collaborator);
        await _repository.SaveChangesAsync();

        var response = _mapper.Map<CollaboratorResponseDto>(collaborator);

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
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

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

        return _mapper.Map<CollaboratorResponseDto>(collaborator);
    }

    public async Task<CollaboratorResponseDto> GetByIdAsync(Guid id)
    {
        var collaborator = await _repository.GetFirstWithIncludesAsync(
            c => c.Id == id,
            q => q.IncludeMultiple(c => c.User, c => c.BusinessField, c => c.Company));

        if (collaborator == null)
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

        return _mapper.Map<CollaboratorResponseDto>(collaborator);
    }

    public async Task<PagedList<CollaboratorResponseDto>> GetPagedAsync(
        int page,
        int size,
        string? search = null,
        CollaboratorStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        Expression<Func<Collaborator, bool>> predicate = c => true;
        if (!string.IsNullOrEmpty(search))
        {
            var searchUpper = search.ToUpperInvariant();
            predicate = c => (c.FullName.Contains(searchUpper) ||
                             c.Phone.Contains(searchUpper) ||
                             (c.Email != null && c.Email.Contains(searchUpper)) ||
                             (c.CollaboratorCode != null && c.CollaboratorCode.Contains(searchUpper)) ||
                             // Tìm theo lĩnh vực kinh doanh: khớp cả cột denormalized
                             // (bản ghi cũ) lẫn tên trong bảng BusinessFields (tên hiển thị).
                             (c.BusinessFieldName != null && c.BusinessFieldName.Contains(searchUpper)) ||
                             (c.BusinessField != null && c.BusinessField.Name.Contains(searchUpper)) ||
                             (c.BusinessField != null && c.BusinessField.NormalizedName.Contains(searchUpper)))
                             && (!status.HasValue || c.Status == status.Value)
                             && (!fromDate.HasValue || c.CreatedAt >= fromDate.Value.Date.ToUniversalTime())
                             && (!toDate.HasValue || c.CreatedAt < toDate.Value.Date.AddDays(1).ToUniversalTime());
        }
        else
        {
            predicate = c => (!status.HasValue || c.Status == status.Value)
                        && (!fromDate.HasValue || c.CreatedAt >= fromDate.Value.Date.ToUniversalTime())
                        && (!toDate.HasValue || c.CreatedAt < toDate.Value.Date.AddDays(1).ToUniversalTime());
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

        return new PagedList<CollaboratorResponseDto>(
            _mapper.Map<List<CollaboratorResponseDto>>(paged.Items),
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
                c.FullName.Contains(s) ||
                c.Phone.Contains(s) ||
                (c.Email != null && c.Email.Contains(s)) ||
                (c.CollaboratorCode != null && c.CollaboratorCode.Contains(s)) ||
                (c.BusinessFieldName != null && c.BusinessFieldName.Contains(s)) ||
                (c.BusinessField != null && c.BusinessField.Name.Contains(s)));
        }

        query = query.IncludeMultiple(c => c.BusinessField, c => c.Company).OrderByDescending(c => c.CreatedAt);

        var paged = await PagedList<Collaborator>.CreateAsync(query, page, size);

        return new PagedList<CollaboratorResponseDto>(
            _mapper.Map<List<CollaboratorResponseDto>>(paged.Items),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);
    }

    public async Task ApproveAsync(Guid id)
    {
        var collaborator = await _repository.GetByIdAsync(id);
        if (collaborator == null)
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

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
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

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
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

        _repository.Delete(collaborator);
        await _repository.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid id)
    {
        // GetByIdIncludingDeletedAsync bỏ qua global soft-delete filter → tìm được record đã xóa mềm.
        var collaborator = await _repository.GetByIdIncludingDeletedAsync(id);
        if (collaborator == null || !collaborator.IsDeleted)
            throw CollaboratorException.NotFound(_exceptionLocalizer, id);

        _repository.Restore(collaborator);
        await _repository.SaveChangesAsync();
    }
}