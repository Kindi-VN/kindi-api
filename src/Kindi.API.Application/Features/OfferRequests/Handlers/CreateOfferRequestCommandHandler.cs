// src/Kindi.API.Application/Features/OfferRequests/Commands/CreateOfferRequestCommandHandler.cs
using AutoMapper;
using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Features.OfferRequests.Commands;

public class CreateOfferRequestCommandHandler : IRequestHandler<CreateOfferRequestCommand, OfferRequestResponseDto>
{
    private readonly IRepository<OfferRequest> _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly IReferralService _referralService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateOfferRequestCommandHandler(
        IRepository<OfferRequest> repository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService,
        IReferralService referralService,
        IStringLocalizer<SharedResource> localizer)
    {
        _repository = repository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
        _referralService = referralService;
        _localizer = localizer;
    }

    public async Task<OfferRequestResponseDto> Handle(CreateOfferRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Get or create user
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // Khách chưa đăng nhập bắt buộc nhập thông tin liên hệ để tạo tài khoản và liên hệ
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new BusinessException(_localizer["OfferRequest_FullNameRequired"]);

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new BusinessException(_localizer["OfferRequest_PhoneRequired"]);

            var userIdGuid = await _userService.GetOrCreateUserAsync(
                request.FullName,
                request.Phone,
                request.Email
            );

            userId = userIdGuid.ToString();
        }
        else
        {
            // Người đã đăng nhập không phải nhập lại thông tin → bù từ hồ sơ tài khoản
            // để bản ghi luôn có họ tên/SĐT/email cho admin liên hệ.
            var account = await _userService.GetCurrentUserAsync();
            if (account != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName)) request.FullName = account.FullName;
                if (string.IsNullOrWhiteSpace(request.Phone)) request.Phone = account.Phone ?? string.Empty;
                if (string.IsNullOrWhiteSpace(request.Zalo)) request.Zalo = account.Phone;
                if (string.IsNullOrWhiteSpace(request.Email)) request.Email = account.Email;
            }
        }

        // 2. Map to entity
        var entity = _mapper.Map<OfferRequest>(request);
        entity.OfferRequestCode = CodeGenerator.Generate("OFR");
        entity.UserId = Guid.Parse(userId);
        entity.Status = OfferStatus.Pending;
        // Mã CTV của link chia sẻ khách dùng để tạo yêu cầu (mã không tồn tại thì bỏ qua)
        entity.ReferralCode = await _referralService.ResolveAsync(request.ReferralCode);

        // 3. Save to database
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // 4. Map to response
        var response = _mapper.Map<OfferRequestResponseDto>(entity);
        return response;
    }
}