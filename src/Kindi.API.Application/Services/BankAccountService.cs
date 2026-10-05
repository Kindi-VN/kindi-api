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
using System.Security.Cryptography;

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
            // Đổi thông tin thì mã đối chiếu cũ không còn dùng được.
            account.VerificationCode = null;
            account.VerificationCodeIssuedAt = null;
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
        if (request.IsVerified)
        {
            // Đã xác minh thì mã đối chiếu không cần nữa.
            account.VerificationCode = null;
            account.VerificationCodeIssuedAt = null;
        }
        account.VerifiedBy = request.IsVerified ? _currentUserService.UserName : null;
        account.Note = request.Note?.Trim();
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedBy = _currentUserService.UserName;

        _bankAccountRepository.Update(account);
        await _bankAccountRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã cập nhật xác minh ngân hàng của tài khoản {UserId}: {IsVerified}", userId, request.IsVerified);

        return Map(account);
    }

    /// <inheritdoc />
    public async Task<BankAccountVerificationCodeResponse> IssueVerificationCodeAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        var account = await _bankAccountRepository.GetFirstWithIncludesAsync(
            x => x.UserId == userId,
            query => query.Include(x => x.User),
            cancellationToken) ?? throw new BusinessException(_localizer["Payout_BankAccountNotFound"]);

        // Mã còn hiệu lực thì trả lại mã cũ để thành viên không chuyển khoản theo nhiều nội dung khác nhau.
        if (!string.IsNullOrWhiteSpace(account.VerificationCode)
            && account.VerificationCodeIssuedAt.HasValue
            && account.VerificationCodeIssuedAt.Value.AddDays(CodeLifetimeDays) > DateTime.UtcNow)
        {
            return BuildCodeResponse(account);
        }

        account.VerificationCode = BuildVerificationCode(account.AccountNumber);
        account.VerificationCodeIssuedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedBy = _currentUserService.UserName;

        _bankAccountRepository.Update(account);
        await _bankAccountRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã tạo mã đối chiếu chuyển khoản cho tài khoản {UserId}", userId);

        return BuildCodeResponse(account);
    }

    private Guid CurrentUserId()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new BusinessException(_localizer["Payout_UserInvalid"]);

        return userId;
    }

    /// <summary>Số ngày mã đối chiếu còn hiệu lực.</summary>
    private const int CodeLifetimeDays = 30;

    /// <summary>Số tiền gợi ý chuyển khoản để xác minh (đồng).</summary>
    private const decimal SuggestedTransferAmount = 1000;

    /// <summary>Bảng ký tự sinh mã — bỏ các ký tự dễ nhầm khi đọc (I, O, 0, 1).</summary>
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Mã đối chiếu gồm tiền tố KINDI, 4 số cuối tài khoản và 6 ký tự ngẫu nhiên.</summary>
    private static string BuildVerificationCode(string accountNumber)
    {
        var digits = new string(accountNumber.Where(char.IsDigit).ToArray());
        var tail = digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');
        var random = new char[6];
        for (var i = 0; i < random.Length; i++)
            random[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];

        return $"KINDI{tail}{new string(random)}";
    }

    /// <summary>Thông tin mã đối chiếu trả cho thành viên: mã, nội dung chuyển khoản, số tiền và hạn.</summary>
    private static BankAccountVerificationCodeResponse BuildCodeResponse(UserBankAccount account) => new()
    {
        VerificationCode = account.VerificationCode ?? string.Empty,
        TransferContent = account.VerificationCode ?? string.Empty,
        Amount = SuggestedTransferAmount,
        IssuedAt = account.VerificationCodeIssuedAt,
        ExpiresAt = account.VerificationCodeIssuedAt?.AddDays(CodeLifetimeDays)
    };

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
        VerificationCode = account.VerificationCode,
        VerificationCodeIssuedAt = account.VerificationCodeIssuedAt,
        UpdatedAt = account.UpdatedAt ?? account.CreatedAt
    };
}
