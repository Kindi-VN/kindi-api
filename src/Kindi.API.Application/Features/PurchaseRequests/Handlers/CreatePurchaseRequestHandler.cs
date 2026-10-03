using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Errors;
using Kindi.API.Application.Features.PurchaseRequests.Commands;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Errors;
using MediatR;

namespace Kindi.API.Application.Features.PurchaseRequests.Handlers;

public class CreatePurchaseRequestHandler : IRequestHandler<CreatePurchaseRequestCommand, PurchaseRequestResponseDto>
{
    private readonly IRepository<PurchaseRequest> _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly IReferralService _referralService;

    public CreatePurchaseRequestHandler(
        IRepository<PurchaseRequest> repository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService,
        IReferralService referralService)
    {
        _repository = repository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
        _referralService = referralService;
    }

    public async Task<PurchaseRequestResponseDto> Handle(CreatePurchaseRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy UserId từ token (string)
        var userIdString = _currentUserService.UserId;
        Guid userId;

        // 2. Khách chưa đăng nhập: dùng lại tài khoản theo SĐT/email, chỉ tạo mới khi chưa có
        if (string.IsNullOrEmpty(userIdString))
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new AppException(PurchaseRequestError.FullNameRequired);

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new AppException(PurchaseRequestError.PhoneRequired);

            var existingUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
            userId = existingUser?.Id ?? await _userService.GetOrCreateUserAsync(
                request.FullName,
                request.Phone,
                request.Email);
        }
        else
        {
            userId = Guid.Parse(userIdString);

            // Người đã đăng nhập không phải nhập lại thông tin liên hệ → bù từ hồ sơ tài khoản
            var account = await _userService.GetCurrentUserAsync();
            if (account != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName)) request.FullName = account.FullName;
                if (string.IsNullOrWhiteSpace(request.Phone)) request.Phone = account.Phone ?? string.Empty;
                if (string.IsNullOrWhiteSpace(request.Zalo)) request.Zalo = account.Phone;
                if (string.IsNullOrWhiteSpace(request.Email)) request.Email = account.Email;
            }
        }

        // 3. Tạo entity và gán UserId
        var entity = _mapper.Map<PurchaseRequest>(request);
        entity.PurchaseRequestCode = CodeGenerator.Generate("PRQ");
        entity.UserId = userId;
        entity.Status = PurchaseRequestStatus.Pending;
        // Mã chia sẻ của link dùng để tạo yêu cầu: lần đầu thì ghi nhận vào tài khoản,
        // các lần sau lấy mã đã ghi nhận (mã không tồn tại thì bỏ qua).
        entity.ReferralCode = await _referralService.ResolveForUserAsync(entity.UserId, request.ReferralCode);

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        // Ghi nhận phát sinh giới thiệu của yêu cầu tìm hàng.
        await _referralService.RecordEventAsync(entity.ReferralCode, entity.UserId, ReferralEventType.PurchaseRequest,
            entity.Id, entity.PurchaseRequestCode, entity.ExpectedPrice);

        // Thông tin cá nhân chỉ lưu ở bảng Users — form gửi lên thì cập nhật vào tài khoản.
        await _userService.UpdatePersonalInfoAsync(entity.UserId, request.FullName, request.Phone, request.Email, request.Zalo);

        var response = _mapper.Map<PurchaseRequestResponseDto>(entity);
        var personalInfo = await _userService.GetPersonalInfoAsync(entity.UserId);
        response.FullName = personalInfo?.FullName ?? string.Empty;
        response.Phone = personalInfo?.Phone ?? string.Empty;
        response.Zalo = personalInfo?.Zalo;
        response.Email = personalInfo?.Email;
        return response;
    }
}