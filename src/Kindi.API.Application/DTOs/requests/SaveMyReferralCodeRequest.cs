namespace Kindi.API.Application.DTOs.requests;

/// <summary>Mã chia sẻ (?ref=) trên link khách đang mở — ghi nhận vào tài khoản đang đăng nhập.</summary>
public class SaveMyReferralCodeRequest
{
    public string? ReferralCode { get; set; }
}
