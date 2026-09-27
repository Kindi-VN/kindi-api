using AutoMapper;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Services;

public class PartnerService : IPartnerService
{
    private readonly IRepository<Partner> _partnerRepo;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IRepository<User> _userRepo;
    private readonly IQueryService _queryService;
    private readonly IRepository<PartnerProduct> _productRepo;
    private readonly IRepository<BusinessField> _businessFieldRepo;
    private readonly Kindi.API.Application.Common.Interfaces.ICompanyService _companyService;

    public PartnerService(
        IRepository<Partner> partnerRepo,
        IRepository<User> userRepo,
        IUserService userService,
        ICurrentUserService currentUserService,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer,
        IQueryService queryService,
        IRepository<PartnerProduct> productRepo,
        IRepository<BusinessField> businessFieldRepo,
        Kindi.API.Application.Common.Interfaces.ICompanyService companyService)
    {
        _partnerRepo = partnerRepo;
        _userRepo = userRepo;
        _userService = userService;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _localizer = localizer;
        _queryService = queryService;
        _productRepo = productRepo;
        _businessFieldRepo = businessFieldRepo;
        _companyService = companyService;
    }

    public async Task<PartnerRegisterResponse> RegisterAsync(PartnerRegisterRequest request)
    {
        // 1. Kiểm tra referral code (nếu có)
        if (!string.IsNullOrEmpty(request.ReferralCode))
        {
            var isValid = await IsReferralCodeValidAsync(request.ReferralCode);
            if (!isValid)
                throw new BusinessException(_localizer["PartnerReferralCodeInvalid"]);
        }

        // 2. Lấy hoặc tạo User
        var userId = _currentUserService.UserId;
        var isPublicRegistration = string.IsNullOrEmpty(userId);
        var isNewAccount = false;
        string? accountUsername = null;

        if (isPublicRegistration)
        {
            // Đăng ký công khai: tạo tài khoản đăng nhập được ngay (username user<sđt>, mật khẩu = SĐT)
            // thay vì mật khẩu ngẫu nhiên như trước — để người đăng ký biết thông tin đăng nhập.
            var existingUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
            isNewAccount = existingUser == null;

            var userGuid = await _userService.GetOrCreateUserWithPhonePasswordAsync(
                request.FullName,
                request.Phone,
                request.Email
            );
            userId = userGuid.ToString();

            if (isNewAccount)
            {
                var createdUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
                accountUsername = createdUser?.Username;
            }
        }

        // 3. Map request -> Partner entity
        var partner = _mapper.Map<Partner>(request);
        // userId luôn có giá trị: người dùng đang đăng nhập, hoặc tài khoản vừa tạo ở nhánh đăng ký công khai
        partner.UserId = Guid.Parse(userId!);

        partner.PartnerCode = GeneratePartnerCode();

        // 4. Map products và gán PartnerId
        var products = _mapper.Map<List<PartnerProduct>>(request.Products);
        foreach (var product in products)
        {
            product.PartnerId = partner.Id;
            product.PartnerProductCode = CodeGenerator.Generate("PRDP");
        }
        partner.Products = products;

        // 5. Form mới không thu thập chính sách hoa hồng — tạo hoa hồng mặc định
        partner.Commission = new PartnerCommission
        {
            Type = CommissionType.Percentage,
            Rate = 0,
            PartnerId = partner.Id,
            PartnerCommissionCode = CodeGenerator.Generate("PCM")
        };

        // 6. Ensure Company created/linked from registration form (do not lose legacy fields)
        var company = await _companyService.AddOrUpdateFromLegacyAsync(
            request.CompanyName, null, request.CompanyAddress, null,
            request.BusinessFieldId, partner.BusinessType, request.CompanySize);
        if (company != null)
        {
            partner.CompanyId = company.Id;
            // keep legacy fields for backward compatibility
            partner.CompanyName = request.CompanyName;
            partner.CompanyAddress = request.CompanyAddress;
            partner.CompanySize = request.CompanySize;
        }

        // 7. Lưu vào DB
        await _partnerRepo.AddAsync(partner);
        await _partnerRepo.SaveChangesAsync();

        // 8. Return response
        var response = _mapper.Map<PartnerRegisterResponse>(partner);

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
    /// Nguồn cung công khai: chỉ những đối tác doanh nghiệp đã được duyệt (Approved/Active)
    /// và chưa xoá. Không lộ SĐT/email — khách liên hệ qua form của app.
    /// </summary>
    public async Task<PagedList<PublicPartnerResponseDto>> GetPublicPagedAsync(PublicPartnerQueryDto query)
    {
        var search = NormalizePartnerFilter(query.Search)?.ToLowerInvariant();

        var q = _queryService.GetAllNoTracking<Partner>()
            .Where(x => x.Status == PartnerStatus.Approved || x.Status == PartnerStatus.Active)
            .WhereIf(query.BusinessFieldId.HasValue, x => x.BusinessFieldId == query.BusinessFieldId!.Value)
            .WhereIf(!string.IsNullOrEmpty(search), x =>
                x.CompanyName.ToLower().Contains(search!) ||
                x.FullName.ToLower().Contains(search!) ||
                (x.CompanyAddress != null && x.CompanyAddress.ToLower().Contains(search!)) ||
                (x.BusinessField != null && x.BusinessField.Name.ToLower().Contains(search!)) ||
                x.Products.Any(p => !p.IsDeleted && p.Name.ToLower().Contains(search!)))
            .Include(x => x.BusinessField)
            .Include(x => x.Products);

        var paged = await q.ToPagedListAsync(query.Page, query.PageSize, null, null, defaultSortBy: "CreatedAt");
        return _mapper.MapPagedList<Partner, PublicPartnerResponseDto>(paged);
    }

    /// <summary>Chuẩn hoá filter: rỗng / "undefined" / "null" / "all" coi như không lọc.</summary>
    private static string? NormalizePartnerFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "undefined" or "null" or "nan" or "all" => null,
            _ => trimmed
        };
    }

    private string GeneratePartnerCode()
    {
        // Format: PART-{DateTime:yyMMdd}-{Random4Digits}
        var datePart = DateTime.Now.ToString("yyMMdd");
        var randomPart = new Random().Next(1000, 9999).ToString();
        return $"PART-{datePart}-{randomPart}";
    }

    public async Task<bool> IsReferralCodeValidAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        // Mã giới thiệu có thể là SĐT hoặc UserCode (không phân biệt hoa/thường với code)
        var trimmed = code.Trim();
        var user = await _userRepo.GetFirstAsync(u =>
            u.Phone == trimmed ||
            (u.UserCode != null && u.UserCode.ToUpper() == trimmed.ToUpper()));
        return user != null;
    }

    public async Task<PagedList<PartnerResponseDto>> GetPagedAsync(PartnerFilterRequest filter)
    {
        // isDeleted=true bỏ global soft-delete filter để lấy cả bản ghi đã xóa.
        var q = filter.IsDeleted == true
            ? _queryService.GetQueryableNoTracking<Partner>().IgnoreQueryFilters().Where(x => x.IsDeleted)
            : _queryService.GetAllNoTracking<Partner>();

        var searchUpper = filter.Search?.ToUpperInvariant();
        q = q
            // Search filter
            .WhereIf(!string.IsNullOrEmpty(filter.Search), x =>
                x.FullName.Contains(filter.Search!) ||
                x.Email.Contains(filter.Search!) ||
                x.Phone.Contains(filter.Search!) ||
                x.CompanyName.Contains(filter.Search!) ||
                x.CompanyTax.Contains(filter.Search!) ||
                x.PartnerCode.Contains(filter.Search!) ||
                (x.ReferralCode != null && x.ReferralCode.Contains(filter.Search!)) ||
                // Tìm theo lĩnh vực kinh doanh — Partner không có cột tên denormalized
                // nên phải qua nav (EF dịch thành LEFT JOIN).
                (x.BusinessField != null && x.BusinessField.Name.Contains(filter.Search!)) ||
                (x.BusinessField != null && x.BusinessField.NormalizedName.Contains(searchUpper!)))
            // Status filter
            .WhereIf(filter.Status.HasValue, x => x.Status == filter.Status!.Value)
            // Date range filter
            .WhereIf(filter.FromDate.HasValue, x => x.CreatedAt >= filter.FromDate!.Value.Date.ToUniversalTime())
            .WhereIf(filter.ToDate.HasValue, x => x.CreatedAt < filter.ToDate!.Value.Date.AddDays(1).ToUniversalTime())
            // Lọc trước rồi mới include: phần join chỉ chạy trên tập bản ghi còn lại
            // Include nav lĩnh vực để map BusinessFieldName trong PartnerResponseDto
            .Include(x => x.BusinessField);

        var result = await q.ToPagedListAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SortBy,
            filter.SortOrder,
            defaultSortBy: "CreatedAt"
        );

        // Temporary migration: for partners that still have legacy company fields but no CompanyId,
        // ensure a Company record exists and link it.
        foreach (var item in result.Items)
        {
            if (!item.CompanyId.HasValue && (!string.IsNullOrWhiteSpace(item.CompanyName) || !string.IsNullOrWhiteSpace(item.CompanyTax)))
            {
                var company = await _companyService.AddOrUpdateFromLegacyAsync(
                    item.CompanyName, item.CompanyTax, item.CompanyAddress, item.CompanyWebsite, item.BusinessFieldId, item.BusinessType, item.CompanySize);
                if (company != null)
                {
                    // Persist CompanyId back to the tracked Partner entity
                    var tracked = filter.IsDeleted == true
                        ? await _partnerRepo.GetByIdIncludingDeletedAsync(item.Id)
                        : await _partnerRepo.GetByIdAsync(item.Id);
                    if (tracked != null)
                    {
                        tracked.CompanyId = company.Id;
                        _partnerRepo.Update(tracked);
                        await _partnerRepo.SaveChangesAsync();
                        // update the item in the in-memory result for response
                        item.CompanyId = company.Id;
                    }
                }
            }
        }

        return _mapper.MapPagedList<Partner, PartnerResponseDto>(result);
    }

    public async Task<PartnerDetailResponseDto?> GetDetailAsync(Guid id)
    {
        var entity = await _partnerRepo.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query
                .Include(x => x.User)
                .Include(x => x.BusinessField)
                .Include(x => x.Commission)
                // ThenInclude để PartnerProductDto.businessFieldName có dữ liệu
                .Include(x => x.Products).ThenInclude(p => p.BusinessField));

        if (entity == null)
            return null;

        // Ensure company created/linked from legacy fields when needed
        if (!entity.CompanyId.HasValue && (!string.IsNullOrWhiteSpace(entity.CompanyName) || !string.IsNullOrWhiteSpace(entity.CompanyTax)))
        {
            var company = await _companyService.AddOrUpdateFromLegacyAsync(
                entity.CompanyName, entity.CompanyTax, entity.CompanyAddress, entity.CompanyWebsite, entity.BusinessFieldId, entity.BusinessType, entity.CompanySize);
            if (company != null)
            {
                entity.CompanyId = company.Id;
                await _partnerRepo.SaveChangesAsync();
            }
        }

        return _mapper.Map<PartnerDetailResponseDto>(entity);
    }

    public async Task<PartnerResponseDto> ApproveAsync(Guid id)
    {
        var entity = await _partnerRepo.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.BusinessField));
        if (entity == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        if (entity.Status != PartnerStatus.Pending)
            throw new InvalidOperationException(_localizer["Partner_InvalidStatusTransition"]);

        entity.Status = PartnerStatus.Approved;
        entity.ApprovedAt = DateTime.UtcNow;

        _partnerRepo.Update(entity);
        await _partnerRepo.SaveChangesAsync();

        return _mapper.Map<PartnerResponseDto>(entity);
    }

    public async Task<PartnerResponseDto> RejectAsync(Guid id)
    {
        var entity = await _partnerRepo.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.BusinessField));
        if (entity == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        if (entity.Status != PartnerStatus.Pending)
            throw new InvalidOperationException(_localizer["Partner_InvalidStatusTransition"]);

        entity.Status = PartnerStatus.Rejected;

        _partnerRepo.Update(entity);
        await _partnerRepo.SaveChangesAsync();

        return _mapper.Map<PartnerResponseDto>(entity);
    }

    public async Task<PartnerResponseDto> ActivateAsync(Guid id)
    {
        var entity = await _partnerRepo.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.BusinessField));
        if (entity == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        if (entity.Status != PartnerStatus.Approved)
            throw new InvalidOperationException(_localizer["Partner_InvalidStatusTransition"]);

        entity.Status = PartnerStatus.Active;

        _partnerRepo.Update(entity);
        await _partnerRepo.SaveChangesAsync();

        return _mapper.Map<PartnerResponseDto>(entity);
    }

    public async Task<PartnerResponseDto> UpdateAsync(Guid id, UpdatePartnerDto request)
    {
        var entity = await _partnerRepo.GetFirstWithIncludesAsync(
            x => x.Id == id,
            query => query.Include(x => x.BusinessField));

        if (entity == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        // Partial update: field nào null thì AutoMapper giữ nguyên giá trị cũ.
        _mapper.Map(request, entity);

        // Lĩnh vực kinh doanh: validate tồn tại trước khi gán (Partner không có cột tên
        // denormalized nên chỉ cần chốt Id, tên lấy qua nav).
        if (request.BusinessFieldId.HasValue)
        {
            var field = await _businessFieldRepo.GetFirstAsync(
                b => b.Id == request.BusinessFieldId.Value && !b.IsDeleted);

            if (field == null)
                throw new BadRequestException("Lĩnh vực hoạt động không tồn tại");

            entity.BusinessFieldId = field.Id;
        }

        // KHÔNG gọi _partnerRepo.Update(entity): entity đang được tracking nên EF tự phát
        // hiện thay đổi (và Update còn lưu đồng bộ ngay bên trong, gây lưu thừa).
        await _partnerRepo.SaveChangesAsync();

        return _mapper.Map<PartnerResponseDto>(entity);
    }

    // ===== Sản phẩm / dịch vụ của đối tác =====
    // Tách thành API riêng thay vì gửi kèm trong PUT /partners/{id}: mỗi sản phẩm giữ
    // nguyên Id và mã PRDP khi sửa, không bị xóa mềm rồi tạo lại mỗi lần lưu partner.

    public async Task<PartnerProductDto> AddProductAsync(Guid partnerId, CreatePartnerProductDto request)
    {
        var partner = await _partnerRepo.GetByIdAsync(partnerId);
        if (partner == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        var product = _mapper.Map<PartnerProduct>(request);
        product.PartnerId = partnerId;
        product.PartnerProductCode = CodeGenerator.Generate("PRDP");

        await _productRepo.AddAsync(product);

        return _mapper.Map<PartnerProductDto>(product);
    }

    public async Task<PartnerProductDto> UpdateProductAsync(
        Guid partnerId,
        Guid productId,
        UpdatePartnerProductDto request)
    {
        var product = await GetProductOfPartnerAsync(partnerId, productId);

        // Partial update: field null giữ nguyên.
        _mapper.Map(request, product);

        await _productRepo.SaveChangesAsync();

        return _mapper.Map<PartnerProductDto>(product);
    }

    public async Task DeleteProductAsync(Guid partnerId, Guid productId)
    {
        var product = await GetProductOfPartnerAsync(partnerId, productId);

        // Xóa mềm — bản ghi vẫn còn trong DB với IsDeleted = true.
        _productRepo.Delete(product);
    }

    /// <summary>
    /// Lấy sản phẩm và xác nhận nó thuộc đúng partner trong route.
    /// </summary>
    private async Task<PartnerProduct> GetProductOfPartnerAsync(Guid partnerId, Guid productId)
    {
        var product = await _productRepo.GetFirstWithIncludesAsync(
            p => p.Id == productId && p.PartnerId == partnerId,
            query => query.Include(p => p.BusinessField));

        if (product == null)
            throw new NotFoundException(_localizer["Partner_ProductNotFound"]);

        return product;
    }

    public async Task DeleteAsync(Guid id)
    {
        // GetByIdAsync tôn trọng global soft-delete filter → chỉ xóa được bản ghi đang sống.
        var entity = await _partnerRepo.GetByIdAsync(id);
        if (entity == null)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        // IRepository.Delete chuyển thành IsDeleted = true (không hard delete).
        _partnerRepo.Delete(entity);
        await _partnerRepo.SaveChangesAsync();
    }

    public async Task<PartnerResponseDto> RestoreAsync(Guid id)
    {
        // Bỏ global soft-delete filter để tìm được bản ghi đã xóa; Include nav lĩnh vực
        // để response sau khi khôi phục vẫn có BusinessFieldName.
        var entity = await _queryService.GetQueryable<Partner>()
            .IgnoreQueryFilters()
            .Include(x => x.BusinessField)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || !entity.IsDeleted)
            throw new NotFoundException(_localizer["Partner_NotFound"]);

        entity.IsDeleted = false;
        _partnerRepo.Update(entity);
        await _partnerRepo.SaveChangesAsync();

        return _mapper.Map<PartnerResponseDto>(entity);
    }

    public async Task<PagedList<PartnerResponseDto>> GetPagedDeletedAsync(int pageNumber, int pageSize, string? search = null)
    {
        var filter = new PartnerFilterRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            IsDeleted = true
        };

        return await GetPagedAsync(filter);
    }
}