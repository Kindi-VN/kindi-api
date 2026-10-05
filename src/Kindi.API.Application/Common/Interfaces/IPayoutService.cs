namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

/// <summary>Giải ngân hoa hồng: ví của thành viên, yêu cầu rút sớm và các kỳ chi trả theo tháng.</summary>
public interface IPayoutService
{
    /// <summary>Cấu hình phí rút sớm dùng chung.</summary>
    Task<PayoutSettingResponse> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Cập nhật cấu hình phí rút sớm dùng chung.</summary>
    Task<PayoutSettingResponse> SaveSettingsAsync(SavePayoutSettingRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ví hoa hồng của chính người gọi.</summary>
    Task<MyWalletResponse> GetWalletAsync(CancellationToken cancellationToken = default);

    /// <summary>Danh sách chi trả hoa hồng.</summary>
    Task<PagedList<PayoutStatementResponse>> GetPagedAsync(PayoutQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Danh sách chi trả hoa hồng của chính người gọi (luôn ép UserId theo tài khoản đang đăng nhập).</summary>
    Task<PagedList<PayoutStatementResponse>> GetMyPagedAsync(PayoutQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Gửi yêu cầu rút hoa hồng sớm; phí rút sớm được tính theo hạng và cấu hình chung.</summary>
    Task<PayoutStatementResponse> CreateWithdrawalAsync(CreateWithdrawalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Duyệt một lần chi trả.</summary>
    Task<PayoutStatementResponse> ApproveAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Từ chối một lần chi trả.</summary>
    Task<PayoutStatementResponse> RejectAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Xác nhận đã chuyển khoản một lần chi trả.</summary>
    Task<PayoutStatementResponse> MarkPaidAsync(Guid id, ProcessPayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Thành viên huỷ yêu cầu rút sớm đang chờ duyệt của chính mình.</summary>
    Task<PayoutStatementResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Danh sách kỳ giải ngân theo tháng.</summary>
    Task<PagedList<PayoutPeriodResponse>> GetPeriodsAsync(PayoutPeriodQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>Mở kỳ giải ngân của một tháng (đã có thì trả về kỳ đang có).</summary>
    Task<PayoutPeriodResponse> OpenPeriodAsync(CreatePayoutPeriodRequest request, CancellationToken cancellationToken = default);

    /// <summary>Chốt sổ một kỳ: ghi nhận hoa hồng của từng thành viên thành các lần chi trả.</summary>
    Task<PayoutPeriodResponse> ClosePeriodAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Xác nhận đã chi trả toàn bộ các lần chi trả của một kỳ.</summary>
    Task<PayoutPeriodResponse> PayPeriodAsync(Guid id, CancellationToken cancellationToken = default);
}
