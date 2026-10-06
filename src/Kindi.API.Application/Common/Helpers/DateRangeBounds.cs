namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Chuẩn hoá khoảng ngày dùng cho bộ lọc và thao tác xoá: UI gửi ngày dạng YYYY-MM-DD nên mốc KẾT THÚC
/// phải phủ hết ngày đó — nếu không, bản ghi phát sinh trong chính ngày kết thúc sẽ bị loại
/// (lỗi "lọc tới 06/10 mà không thấy log của ngày 06/10").
/// </summary>
public static class DateRangeBounds
{
    /// <summary>Mốc bắt đầu: chỉ quy về UTC, giữ nguyên phần giờ nếu client gửi kèm.</summary>
    public static DateTime? NormalizeFrom(DateTime? value)
        => value?.ToUniversalTime();

    /// <summary>
    /// Mốc kết thúc: nếu client chỉ gửi NGÀY (00:00:00) thì lấy hết ngày đó
    /// (23:59:59.9999999 giờ địa phương → UTC); gửi kèm giờ thì giữ nguyên.
    /// </summary>
    public static DateTime? NormalizeTo(DateTime? value)
    {
        if (value == null)
            return null;

        var local = value.Value;
        if (local.TimeOfDay == TimeSpan.Zero)
            local = local.Date.AddDays(1).AddTicks(-1);

        return local.ToUniversalTime();
    }
}
