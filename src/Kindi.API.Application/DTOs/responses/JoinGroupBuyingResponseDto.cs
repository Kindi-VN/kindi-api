namespace Kindi.API.Application.DTOs.responses;

/// <summary>
/// Kết quả đăng ký tham gia mua chung. Nếu là khách chưa có tài khoản, hệ thống đã tạo
/// tài khoản từ thông tin liên hệ và trả về username để client thông báo cho khách.
/// </summary>
public class JoinGroupBuyingResponseDto
{
    public Guid ParticipantId { get; set; }
    public int CurrentPeopleCount { get; set; }
    public int TargetPeopleCount { get; set; }
    public int NeededPeopleCount { get; set; }
    public bool IsGroupFull { get; set; }

    /// <summary>Tài khoản vừa được tạo tự động cho khách trong lượt đăng ký này.</summary>
    public bool IsNewAccount { get; set; }
    public string? Username { get; set; }
    /// <summary>true = mật khẩu khởi tạo chính là số điện thoại khách vừa nhập.</summary>
    public bool PasswordIsPhone { get; set; }
    /// <summary>Tài khoản đã tồn tại trước đó (theo SĐT/email) nên không tạo mới.</summary>
    public bool AccountAlreadyExisted { get; set; }

    public string Message { get; set; } = string.Empty;
}
