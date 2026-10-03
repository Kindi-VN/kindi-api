namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Thông tin ngân hàng nhận giải ngân: mỗi thành viên có một thông tin, sửa số tài khoản thì phải
/// được quản trị viên xác minh lại trước khi chi trả.
/// </summary>
public sealed class BankAccountService : IBankAccountService
{
    private readonly IRepository<UserBankAccount> _bankAccountRepository;
    private readonly IQueryService _queryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<BankAccountService> _logger;

    public BankAccountService(
        IRepository<UserBankAccount> bankAccountRepository,
        IQueryService queryService,
        ICurrentUserService currentUserService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<BankAccountService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _queryService = queryService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BankAccountResponse?> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var account = await _bankAccountRepository.GetFirstWithIncludesAsync(
            x => x.UserId == userId,
            query => query.Include(x => x.User),
            cancellationToken);

        return account == null ? null : Map(account);
    }

    /// <inheritdoc />
    public async Task<BankAccountResponse> SaveMineAsync(SaveBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();

        if (string.IsNullOrWhiteSpace(request.BankName))
            throw new BusinessException(_localizer["Payout_BankNameRequired"]);

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
            throw new BusinessException(_localizer["Payout_AccountNumberRequired"]);

        if (string.IsNullOrWhiteSpace(request.AccountHolder))
            throw new BusinessException(_localizer["Payout_AccountHolderRequired"]);

        var account = await _bankAccountRepository.GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var isNew = account == null;
        account ??= new UserBankAccount { UserId = userId };

        var accountNumber = request.AccountNumber.Trim();
        var bankName = request.BankName.Trim();
        var accountHolder = request.AccountHolder.Trim();

        // Đổi số tài khoản, ngân hàng hoặc chủ tài khoản thì thông tin cũ không còn giá trị xác minh.
        var changed = isNew
            || account.AccountNumber != accountNumber
            || account.BankName != bankName
            || account.AccountHolder != accountHolder;

        if (changed)
        {
            account.IsVerified = false;
            account.VerifiedAt = null;
            account.VerifiedBy = null;
            account.Note = null;
        }

        account.BankName = bankName;
        account.Branch = request.Branch?.Trim();
        account.AccountNumber = accountNumber;
        account.AccountHolder = accountHolder;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedBy = _currentUserService.UserName;
        account.IsDeleted = false;

        if (isNew)
        {
            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.UtcNow;
            account.CreatedBy = _currentUserService.UserName;
            await _bankAccountRepository.AddAsync(account, cancellationToken);
        }
        else
        {
            _bankAccountRepository.Update(account);
        }

        await _bankAccountRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã lưu thông tin ngân hàng của tài khoản {UserId}", userId);

        var saved = await _bankAccountRepository.GetFirstWithIncludesAsync(
            x => x.UserId == userId,
            query => query.Include(x => x.User),
            cancellationToken);

        return Map(saved!);
    }

    /// <inheritdoc />
    public async Task<PagedList<BankAccountResponse>> GetPagedAsync(BankAccountQueryDto query, CancellationToken cancellationToken = default)
    {
        var accounts = _queryService.GetAllNoTracking<UserBankAccount>()
            .Include(x => x.User)
            .Where(x => !x.IsDeleted);

        if (query.IsVerified.HasValue)
            accounts = accounts.Where(x => x.IsVerified == query.IsVerified.Value);

        var keyword = query.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(keyword))
            accounts = accounts.Where(x =>
                x.AccountNumber.ToLower().Contains(keyword) ||
                x.AccountHolder.ToLower().Contains(keyword) ||
                (x.User != null && (x.User.Username.ToLower().Contains(keyword) || x.User.FullName.ToLower().Contains(keyword))));

        // Thông tin chờ xác minh lên trước để quản trị viên xử lý nhanh.
        accounts = accounts
            .OrderBy(x => x.IsVerified)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt);

        var paged = await PagedList<UserBankAccount>.CreateAsync(accounts, query.PageNumber, query.PageSize);
        return new PagedList<BankAccountResponse>(paged.Items.Select(Map).ToList(), paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    /// <inheritdoc />
    public async Task<BankAccountResponse> VerifyAsync(Guid userId, VerifyBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _bankAccountRepository.GetFirstWithIncludesAsync(
            x => x.UserId == userId,
            query => query.Include(x => x.User),
            cancellationToken) ?? throw new BusinessException(_localizer["Payout_BankAccountNotFound"]);

        account.IsVerified = request.IsVerified;
        account.VerifiedAt = request.IsVerified ? DateTime.UtcNow : null;
        account.VerifiedBy = request.IsVerified ? _currentUserService.UserName : null;
        account.Note = request.Note?.Trim();
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedBy = _currentUserService.UserName;

        _bankAccountRepository.Update(account);
        await _bankAccountRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã cập nhật xác minh ngân hàng của tài khoản {UserId}: {IsVerified}", userId, request.IsVerified);

        return Map(account);
    }

    private Guid CurrentUserId()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new BusinessException(_localizer["Payout_UserInvalid"]);

        return userId;
    }

    private static BankAccountResponse Map(UserBankAccount account) => new()
    {
        Id = account.Id,
        UserId = account.UserId,
        Username = account.User?.Username,
        FullName = account.User?.FullName,
        BankName = account.BankName,
        Branch = account.Branch,
        AccountNumber = account.AccountNumber,
        AccountHolder = account.AccountHolder,
        IsVerified = account.IsVerified,
        VerifiedAt = account.VerifiedAt,
        VerifiedBy = account.VerifiedBy,
        Note = account.Note,
        UpdatedAt = account.UpdatedAt ?? account.CreatedAt
    };
}
