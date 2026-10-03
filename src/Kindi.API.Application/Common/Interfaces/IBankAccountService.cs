namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

/// <summary>Thông tin ngân hàng nhận giải ngân của thành viên.</summary>
public interface IBankAccountService
{
    /// <summary>Thông tin ngân hàng của chính người gọi; trống khi chưa nhập.</summary>
    Task<BankAccountResponse?> GetMineAsync(CancellationToken cancellationToken = default);

    /// <summary>Lưu thông tin ngân hàng của chính người gọi (sửa số tài khoản thì phải xác minh lại).</summary>
    Task<BankAccountResponse> SaveMineAsync(SaveBankAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>Danh sách thông tin ngân hàng để quản trị viên xác minh.</summary>
    Task<PagedList<BankAccountResponse>> GetPagedAsync(BankAccountQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Ghi nhận xác minh thông tin ngân hàng của một tài khoản.</summary>
    Task<BankAccountResponse> VerifyAsync(Guid userId, VerifyBankAccountRequest request, CancellationToken cancellationToken = default);
}
