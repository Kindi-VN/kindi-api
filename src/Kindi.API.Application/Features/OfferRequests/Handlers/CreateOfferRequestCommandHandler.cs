using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Errors;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Errors;
using MediatR;

namespace Kindi.API.Application.Features.OfferRequests.Commands;

public class CreateOfferRequestCommandHandler : IRequestHandler<CreateOfferRequestCommand, OfferRequestResponseDto>
{
    private readonly IRepository<OfferRequest> _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;
    private readonly IReferralService _referralService;

    public CreateOfferRequestCommandHandler(
        IRepository<OfferRequest> repository,
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

    public async Task<OfferRequestResponseDto> Handle(CreateOfferRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy UserId từ token, khách chưa đăng nhập thì dùng lại tài khoản theo SĐT/email
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new AppException(OfferRequestError.FullNameRequired);

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new AppException(OfferRequestError.PhoneRequired);

            var existingUser = await _userService.FindByPhoneOrEmailAsync(request.Phone, request.Email);
            userId = existingUser != null
                ? existingUser.Id.ToString()
                : (await _userService.GetOrCreateUserAsync(request.FullName, request.Phone, request.Email)).ToString();
        }
        else
        {
            // Người đã đăng nhập không phải nhập lại thông tin → bù từ hồ sơ tài khoản
            var account = await _userService.GetCurrentUserAsync();
            if (account != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName)) request.FullName = account.FullName;
                if (string.IsNullOrWhiteSpace(request.Phone)) request.Phone = account.Phone ?? string.Empty;
                if (string.IsNullOrWhiteSpace(request.Zalo)) request.Zalo = account.Zalo ?? account.Phone;
                if (string.IsNullOrWhiteSpace(request.Email)) request.Email = account.Email;
            }
        }

        // 2. Map to entity
        var entity = _mapper.Map<OfferRequest>(request);
        entity.OfferRequestCode = CodeGenerator.Generate("OFR");
        entity.UserId = Guid.Parse(userId);
        entity.Status = OfferStatus.Pending;
        // Mã chia sẻ của link dùng để tạo yêu cầu: lần đầu thì ghi nhận vào tài khoản,
        // các lần sau lấy mã đã ghi nhận (mã không tồn tại thì bỏ qua).
        entity.ReferralCode = await _referralService.ResolveForUserAsync(entity.UserId, request.ReferralCode);

        // 3. Save to database
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Thông tin cá nhân chỉ lưu ở bảng Users — form gửi lên thì cập nhật vào tài khoản.
        await _userService.UpdatePersonalInfoAsync(entity.UserId, request.FullName, request.Phone, request.Email, request.Zalo);

        // 4. Map to response
        var response = _mapper.Map<OfferRequestResponseDto>(entity);
        var personalInfo = await _userService.GetPersonalInfoAsync(entity.UserId);
        response.FullName = personalInfo?.FullName ?? string.Empty;
        response.Phone = personalInfo?.Phone ?? string.Empty;
        response.Zalo = personalInfo?.Zalo ?? string.Empty;
        response.Email = personalInfo?.Email;
        return response;
    }
}
