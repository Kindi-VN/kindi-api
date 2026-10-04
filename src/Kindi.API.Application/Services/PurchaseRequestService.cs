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
		var onlyMine = query.MineOnly || !_currentUserService.IsInRole(UserRole.Admin);
		var meId = GetCurrentUserId();

		if (onlyMine && meId == null)
			throw new UnauthorizedException(_localizer["UserNotAuthenticated"]);

		var mineId = meId ?? Guid.Empty;

		// Thông tin cá nhân nằm ở bảng Users — kèm User để tìm kiếm và map DTO.
		var q = _queryService.GetAllNoTracking<PurchaseRequest>()
			.Include(x => x.User)
			.WhereIf(onlyMine, x => x.UserId == mineId)
			.WhereIf(query.Status.HasValue, x => x.Status == query.Status!.Value)
			.WhereIf(!string.IsNullOrEmpty(search), x =>
				(x.PurchaseRequestCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.PurchaseRequestCode), "%" + searchTerm + "%", "\\")) ||
				EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ProductName), "%" + searchTerm + "%", "\\") ||
				(x.ReferralCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.ReferralCode), "%" + searchTerm + "%", "\\")) ||
				(x.User != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.FullName), "%" + searchTerm + "%", "\\")) ||
				(x.User != null && x.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Phone), "%" + searchTerm + "%", "\\")) ||
				(x.User != null && x.User.Email != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(x.User.Email), "%" + searchTerm + "%", "\\")))
			.WhereIf(query.FromDate.HasValue, x => x.CreatedAt >= query.FromDate!.Value.Date.ToUniversalTime())
			.WhereIf(query.ToDate.HasValue, x => x.CreatedAt < query.ToDate!.Value.Date.AddDays(1).ToUniversalTime());

		var pagedEntities = await q.ToPagedListAsync(
			query.Page,
			query.PageSize,
			query.SortBy,
			query.SortOrder,
			defaultSortBy: "CreatedAt"
		);

		var result = _mapper.MapPagedList<PurchaseRequest, PurchaseRequestResponseDto>(pagedEntities);
		await _referralService.FillNamesAsync(result.Items, x => x.ReferralCode, (x, name) => x.ReferralName = name);
		await _referralService.FillNamesAsync(result.Items, x => x.ReferredByCode, (x, name) => x.ReferredByName = name);
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
}