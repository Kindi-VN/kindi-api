namespace Kindi.API.Application.Common.Interfaces;

/// <summary>
/// Mã chia sẻ riêng (refcode) của từng chủ thể: mỗi tài khoản / CTV có một mã riêng dùng để
/// gắn vào link chia sẻ — ai chia sẻ thì bản ghi sinh ra từ link đó ghi nhận mã của người đó
/// (ghi nhận 1 cấp, không tính CTV cấp trên).
/// </summary>
public interface IReferralService
{
    /// <summary>
    /// Mã chia sẻ riêng của tài khoản đang đăng nhập (sinh mới nếu chưa có).
    /// Trả <c>null</c> khi chưa đăng nhập.
    /// </summary>
    Task<string?> GetSharerReferralCodeAsync();

    /// <summary>
    /// Chuẩn hoá mã nhận từ link chia sẻ: mã không tồn tại ở bảng nào thì trả <c>null</c>
    /// (bỏ qua, không chặn người dùng).
    /// </summary>
    Task<string?> ResolveAsync(string? referralCode);

    /// <summary>
    /// Mã chia sẻ dùng cho một bản ghi của <paramref name="userId"/>: lần đầu thì ghi nhận mã
    /// vào tài khoản (<c>Users.ReferredByCode</c>), các lần sau luôn trả mã đã ghi nhận —
    /// mở link của CTV khác cũng không ghi đè. Chưa ghi nhận và mã không hợp lệ thì trả <c>null</c>.
    /// </summary>
    Task<string?> ResolveForUserAsync(Guid userId, string? referralCode);

    /// <summary>
    /// Ghi nhận mã chia sẻ vào tài khoản đang đăng nhập (UI gọi khi khách mở link <c>?ref=</c>).
    /// Trả mã đang ghi nhận của tài khoản, <c>null</c> khi chưa đăng nhập.
    /// </summary>
    Task<string?> AttributeToCurrentUserAsync(string? referralCode);

    /// <summary>Tên chủ thể theo mã (CTV hoặc tài khoản) — dùng để hiển thị ở màn quản trị.</summary>
    Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string?> referralCodes);

    /// <summary>Gắn tên chủ thể vào từng bản ghi theo mã chia sẻ đã ghi nhận (màn quản trị).</summary>
    Task FillNamesAsync<T>(
        IEnumerable<T> items,
        Func<T, string?> getReferralCode,
        Action<T, string> setReferralName);
}
