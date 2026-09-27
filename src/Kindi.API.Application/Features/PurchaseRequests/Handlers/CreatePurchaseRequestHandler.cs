using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Features.PurchaseRequests.Commands;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;
using MediatR;

namespace Kindi.API.Application.Features.PurchaseRequests.Handlers;

public class CreatePurchaseRequestHandler : IRequestHandler<CreatePurchaseRequestCommand, PurchaseRequestResponseDto>
{
    private readonly IRepository<PurchaseRequest> _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;

    public CreatePurchaseRequestHandler(
        IRepository<PurchaseRequest> repository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IUserService userService)
    {
        _repository = repository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<PurchaseRequestResponseDto> Handle(CreatePurchaseRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Lấy UserId từ token (string)
        var userIdString = _currentUserService.UserId;
        Guid userId;

        // 2. Nếu chưa đăng nhập, tạo User ngầm
        if (string.IsNullOrEmpty(userIdString))
        {
            userId = await _userService.GetOrCreateUserAsync(
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

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return _mapper.Map<PurchaseRequestResponseDto>(entity);
    }
}