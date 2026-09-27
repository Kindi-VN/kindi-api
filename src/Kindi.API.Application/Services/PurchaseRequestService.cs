using AutoMapper;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Services;

public class PurchaseRequestService : IPurchaseRequestService
{
	private readonly IRepository<PurchaseRequest> _repository;
	private readonly IMapper _mapper;
	private readonly IStringLocalizer<SharedResource> _localizer;
	private readonly IQueryService _queryService;
	private readonly ICurrentUserService _currentUserService;
	private readonly IUserService _userService;

	public PurchaseRequestService(
		IRepository<PurchaseRequest> repository,
		IMapper mapper,
		IStringLocalizer<SharedResource> stringLocalizer,
		IQueryService queryService,
		ICurrentUserService currentUserService,
		IUserService userService)
	{
		_repository = repository;
		_mapper = mapper;
		_localizer = stringLocalizer;
		_queryService = queryService;
		_currentUserService = currentUserService;
		_userService = userService;
	}

	public async Task<PurchaseRequestResponseDto> CreateAsync(CreatePurchaseRequestDto request)
	{
		var currentUserId = _currentUserService.UserId;

		if (string.IsNullOrEmpty(currentUserId))
		{
			// Khách chưa đăng nhập bắt buộc nhập thông tin liên hệ để admin liên hệ lại
			if (string.IsNullOrWhiteSpace(request.FullName))
				throw new BusinessException(_localizer["PurchaseRequest_FullNameRequired"]);

			if (string.IsNullOrWhiteSpace(request.Phone))
				throw new BusinessException(_localizer["PurchaseRequest_PhoneRequired"]);

			if (string.IsNullOrWhiteSpace(request.Email))
				throw new BusinessException(_localizer["PurchaseRequest_EmailRequired"]);
		}
		else
		{
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

		// Gắn người gửi để admin biết yêu cầu thuộc tài khoản nào (khách để trống)
		if (!string.IsNullOrEmpty(currentUserId))
			entity.UserId = Guid.Parse(currentUserId);

		await _repository.AddAsync(entity);
		await _repository.SaveChangesAsync();

		return _mapper.Map<PurchaseRequestResponseDto>(entity);
	}

	public async Task<PagedList<PurchaseRequestResponseDto>> GetPagedAsync(PurchaseRequestQueryDto query)
	{
		var search = query.Search?.Trim();

		var q = _queryService.GetAllNoTracking<PurchaseRequest>()
			.WhereIf(query.Status.HasValue, x => x.Status == query.Status!.Value)
			.WhereIf(!string.IsNullOrEmpty(search), x =>
				(x.PurchaseRequestCode != null && x.PurchaseRequestCode.Contains(search!)) ||
				x.ProductName.Contains(search!) ||
				x.FullName.Contains(search!) ||
				x.Phone.Contains(search!) ||
				(x.Email != null && x.Email.Contains(search!)))
			.WhereIf(query.FromDate.HasValue, x => x.CreatedAt >= query.FromDate!.Value.Date.ToUniversalTime())
			.WhereIf(query.ToDate.HasValue, x => x.CreatedAt < query.ToDate!.Value.Date.AddDays(1).ToUniversalTime());

		var pagedEntities = await q.ToPagedListAsync(
			query.Page,
			query.PageSize,
			query.SortBy,
			query.SortOrder,
			defaultSortBy: "CreatedAt"
		);

		return _mapper.MapPagedList<PurchaseRequest, PurchaseRequestResponseDto>(pagedEntities);
	}

	public async Task<PurchaseRequestStatusResponseDto> UpdateStatusAsync(
	Guid id,
	UpdatePurchaseRequestStatusDto dto)
	{
		var entity = await _repository.GetByIdAsync(id);
		if (entity == null)
			throw new NotFoundException(_localizer["PurchaseRequestNotFound"]);

		entity.Status = dto.Status;
		entity.UpdatedAt = DateTime.UtcNow;

		_repository.Update(entity);
		await _repository.SaveChangesAsync();

		return _mapper.Map<PurchaseRequestStatusResponseDto>(entity);
	}
}