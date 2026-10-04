namespace Kindi.API.Domain.Enums;

/// <summary>Loại giao dịch được khai doanh thu.</summary>
public enum TransactionType
{
    /// <summary>Yêu cầu mua hàng gửi cho đối tác.</summary>
    PurchaseRequest = 1,

    /// <summary>Yêu cầu mua chung.</summary>
    GroupBuyingRequest = 2
}
