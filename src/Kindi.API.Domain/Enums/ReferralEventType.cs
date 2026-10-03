namespace Kindi.API.Domain.Enums;

/// <summary>Loại phát sinh ghi nhận từ một mã chia sẻ (refcode) — dùng cho thống kê và hoa hồng.</summary>
public enum ReferralEventType
{
    /// <summary>Tài khoản được ghi nhận người giới thiệu (lần đầu mở link của CTV).</summary>
    UserReferred = 1,
    /// <summary>Tạo yêu cầu mua chung.</summary>
    GroupBuyingRequest = 2,
    /// <summary>Tham gia yêu cầu mua chung.</summary>
    GroupBuyingJoin = 3,
    /// <summary>Gửi yêu cầu tìm nhà cung cấp.</summary>
    PurchaseRequest = 4,
    /// <summary>Gửi yêu cầu nhận offer.</summary>
    OfferRequest = 5,
    /// <summary>Xin vào nhóm ngành / hội nhóm.</summary>
    GroupMemberJoin = 6,
    /// <summary>Đăng ký đối tác.</summary>
    PartnerRegister = 7
}
