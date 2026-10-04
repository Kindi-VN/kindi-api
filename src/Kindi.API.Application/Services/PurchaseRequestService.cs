using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Errors;
using Kindi.API.Application.Resources;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Errors;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Services;

public class PurchaseRequestService : IPurchaseRequestService
{

	private readonly IRepository<PurchaseRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IQueryService _queryService;
	private readonly ICurrentUserService _currentUserService;
	private readonly IUserService _userService;
	private readonly IReferralService _referralService;
	private readonly IStringLocalizer<SharedResource> _localizer;

	public PurchaseRequestService(
		IRepository<PurchaseRequest> repository,
		IMapper mapper,
		IQueryService queryService,
		ICurrentUserService currentUserService,
		IUserService userService,
		IReferralService referralService,
		IStringLocalizer<SharedResource> localizer)
	{
		_repository = repository;
		_mapper = mapper;
		_queryService = queryService;
		_currentUserService = currentUserService;
		_userService = userService;
		_referralService = referralService;
		_localizer = localizer;
	}

	public async Task<PurchaseRequestResponseDto> CreateAsync(CreatePurchaseRequestDto request)
	{
		var currentUserId = _currentUserService.UserId;
		Guid userId;

		if (string.IsNullOrEmpty(currentUserId))
		{
			// Khách chưa đăng nhập bắt buộc nhập thông tin liên hệ để admin liên hệ lại
			if (string.IsNullOrWhiteSpace(request.FullName))
				throw new AppException(PurchaseRequestError.FullNameRequired);

			if (string.IsNullOrWhiteSpace(request.Phone))
				throw new AppException(PurchaseRequestError.PhoneRequired);

			if (string.IsNullOrWhiteSpace(request.Email))
				throw new AppException(PurchaseRequestError.EmailRequired);

			// Thông tin cá nhân chỉ lưu ở bảng Users — dùng lại tài khoản theo SĐT/email, chưa có thì tạo mới.
			var resolved = await _userService.ResolvePublicUserAsync(request.FullName, request.Phone, request.Email, request.Zalo);
			userId = resolved.UserId;
		}
		else
		{
			userId = Guid.Parse(currentUserId);

			// Người đã đăng nhập không phải nhập lại thông tin → bù từ hồ sơ tài khoản
			var account = await _userService.GetCurrentUserAsync();
			if (account != null)
			{
				if (string.IsNullOrWhiteSpace(request.FullName)) request.FullName = account.FullName;
				if (string.IsNullOrWhiteSpace(request.Phone)) request.Phone = account.Phone ?? string.Empty;
				if (string.IsNullOrWhiteSpace(request.Zalo)) request.Zalo = account.Phone;
				if (string.IsNullOrWhiteSpace(request.Email)) request.Email = account.Email;
			}
		}

		var entity = _mapper.Map<PurchaseRequest>(request);
		entity.PurchaseRequestCode = CodeGenerator.Generate("PRQ");
		entity.Status = PurchaseRequestStatus.Pending;
		// Yêu cầu thuộc tài khoản người gửi (khách chưa đăng nhập được tạo tài khoản tự động).
		entity.UserId = userId;

		await _repository.AddAsync(entity);
		await _repository.SaveChangesAsync();

		// Thông tin cá nhân của form chỉ ghi vào bảng Users, không lưu ở bảng yêu cầu.
		await _userService.UpdatePersonalInfoAsync(userId, request.FullName, request.Phone, request.Email, request.Zalo);

		var dto = _mapper.Map<PurchaseRequestResponseDto>(entity);
		var personalInfo = await _userService.GetPersonalInfoAsync(userId);
		dto.FullName = personalInfo?.FullName ?? string.Empty;
		dto.Phone = personalInfo?.Phone ?? string.Empty;
		dto.Zalo = personalInfo?.Zalo;
		dto.Email = personalInfo?.Email;
		return dto;
	}

	public async Task<PagedList<PurchaseRequestResponseDto>> GetPagedAsync(PurchaseRequestQueryDto query)
	{
        // Từ khoá đã trim + escape; tìm không phân biệt hoa/thường và không phân biệt dấu.
        var search = query.Search.NormalizeSearchFilter();
        var searchTerm = search?.RemoveVietnameseSign().ToLikeEscaped();

		// Quyền xem: admin thấy tất cả (hoặc chỉ của mình khi truyền mineOnly), người dùng thường chỉ thấy yêu cầu của chính mình.
		var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
		var onlyMine = query.MineOnly || !isAdmin;
		var meId = GetCurrentUserId();

		if (onlyMine && meId == null)
			throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

		var mineId = meId ?? Guid.Empty;

		// Tab "Đã xoá" (chỉ admin): bỏ global soft-delete filter để lấy các yêu cầu đã xoá mềm.
		var onlyDeleted = query.IsDeleted == true && isAdmin;

		var source = onlyDeleted
			? _queryService.GetQueryable<PurchaseRequest>().IgnoreQueryFilters().AsNoTracking().Where(x => x.IsDeleted)
			: _queryService.GetAllNoTracking<PurchaseRequest>();

		// Thông tin cá nhân nằm ở bảng Users — kèm User để tìm kiếm và map DTO.
		var q = source
			.Include(x => x.User)
			.WhereIf(onlyMine, x => x.UserId == mineId)
			.WhereIf(query.Status.HasValue && !onlyDeleted, x => x.Status == query.Status!.Value)
			.WhereIf(query.FromDate.HasValue, x => x.CreatedAt >= query.FromDate!.Value.Date.ToUniversalTime())
			.WhereIf(query.ToDate.HasValue, x => x.CreatedAt < query.ToDate!.Value.Date.AddDays(1).ToUniversalTime());

		// searchField chỉ định thì chỉ dò đúng một trường; bỏ trống thì dò mọi trường như trước.
		q = RequestSearchFilters.ApplyPurchase(q, searchTerm, query.SearchField);

		var pagedEntities = await q.ToPagedListAsync(
			query.Page,
			query.PageSize,
			query.SortBy,
			query.SortOrder,
			defaultSortBy: "CreatedAt"
		);

		var result = _mapper.MapPagedList<PurchaseRequest, PurchaseRequestResponseDto>(pagedEntities);
		await _referralService.FillNamesAsync(result.Items, x => x.RecordReferrerCode, (x, name) => x.RecordReferrerName = name);
		await _referralService.FillNamesAsync(result.Items, x => x.AccountReferrerCode, (x, name) => x.AccountReferrerName = name);
		return result;
	}

	private Guid? GetCurrentUserId()
		=> string.IsNullOrEmpty(_currentUserService.UserId)
			? null
			: Guid.Parse(_currentUserService.UserId!);

	public async Task<PurchaseRequestStatusResponseDto> UpdateStatusAsync(
	Guid id,
	UpdatePurchaseRequestStatusDto dto)
	{
		var entity = await _repository.GetByIdAsync(id);
		if (entity == null)
			throw new AppException(PurchaseRequestError.NotFound);

		entity.Status = dto.Status;
		entity.UpdatedAt = DateTime.UtcNow;

		_repository.Update(entity);
		await _repository.SaveChangesAsync();

		return _mapper.Map<PurchaseRequestStatusResponseDto>(entity);
	}

	/// <summary>Admin xoá mềm một yêu cầu mua hàng (global filter tự ẩn khỏi mọi truy vấn đọc).</summary>
	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		var entity = await _repository.GetByIdAsync(id, cancellationToken);
		if (entity == null || entity.IsDeleted)
			throw new AppException(PurchaseRequestError.NotFound);

		_repository.Delete(entity);
		await _repository.SaveChangesAsync(cancellationToken);
	}

	/// <summary>Admin khôi phục một yêu cầu mua hàng đã xoá mềm.</summary>
	public async Task<PurchaseRequestResponseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
	{
		// GetByIdIncludingDeletedAsync bỏ qua global soft-delete filter → lấy được record đã xoá mềm.
		var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken);
		if (entity == null || !entity.IsDeleted)
			throw new AppException(PurchaseRequestError.NotFound);

		entity.IsDeleted = false;
		entity.UpdatedAt = DateTime.UtcNow;

		_repository.Update(entity);
		await _repository.SaveChangesAsync(cancellationToken);

		return _mapper.Map<PurchaseRequestResponseDto>(entity);
	}
}