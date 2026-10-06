namespace Kindi.API.Application.Common.Helpers;

using Kindi.API.Application.Errors;
using Kindi.API.Domain.Entities;
using Kindi.API.Shared.Errors;

/// <summary>
/// Phạm vi xoá VĨNH VIỄN bản ghi đã xoá mềm: theo danh sách Id (chọn nhiều dòng) HOẶC theo khoảng ngày
/// (mốc so là thời điểm xoá mềm — <c>UpdatedAt</c>, vì bảng không có cột DeletedAt).
/// </summary>
public sealed record PurgeScope(IReadOnlyList<Guid> Ids, DateTime? FromDate, DateTime? ToDate)
{
    /// <summary>true = xoá theo lựa chọn; false = xoá theo khoảng ngày.</summary>
    public bool IsBySelection => Ids.Count > 0;
}

/// <summary>
/// Quy tắc xoá vĩnh viễn ở màn "Đã xoá": luôn bắt buộc có điều kiện (chọn dòng hoặc khoảng ngày) và
/// CHỈ áp lên bản ghi đã xoá mềm — màn thường chỉ được xoá mềm.
/// </summary>
public static class PurgeRules
{
    /// <summary>Số dòng tối đa cho một lần xoá theo lựa chọn.</summary>
    public const int MaxIdsPerRequest = 500;

    /// <summary>
    /// Dựng phạm vi xoá từ yêu cầu: có Id thì xoá theo lựa chọn (bỏ Id rỗng, bỏ trùng); không có Id thì
    /// buộc phải có ít nhất một mốc ngày.
    /// </summary>
    public static PurgeScope Resolve(IEnumerable<Guid>? ids, DateTime? fromDate, DateTime? toDate)
    {
        var distinctIds = ids?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (distinctIds.Count > MaxIdsPerRequest)
            throw new AppException(PurgeError.TooManyIds.WithParams(MaxIdsPerRequest));

        if (distinctIds.Count > 0)
            return new PurgeScope(distinctIds, null, null);

        var normalizedFrom = NormalizeFrom(fromDate);
        var normalizedTo = NormalizeTo(toDate);

        if (normalizedFrom == null && normalizedTo == null)
            throw new AppException(PurgeError.CriteriaRequired);

        if (normalizedFrom.HasValue && normalizedTo.HasValue && normalizedFrom > normalizedTo)
            throw new AppException(PurgeError.RangeInvalid);

        return new PurgeScope(Array.Empty<Guid>(), normalizedFrom, normalizedTo);
    }

    /// <summary>
    /// Lọc đúng các bản ghi sẽ xoá vĩnh viễn: bắt buộc đang ở trạng thái ĐÃ XOÁ MỀM, rồi lọc theo lựa chọn
    /// hoặc theo khoảng ngày xoá mềm.
    /// </summary>
    public static IQueryable<T> Apply<T>(IQueryable<T> query, PurgeScope scope) where T : BaseEntity
    {
        query = query.Where(x => x.IsDeleted);

        return scope.IsBySelection
            ? query.Where(x => scope.Ids.Contains(x.Id))
            : query.Where(x => (!scope.FromDate.HasValue || (x.UpdatedAt ?? x.CreatedAt) >= scope.FromDate.Value)
                            && (!scope.ToDate.HasValue || (x.UpdatedAt ?? x.CreatedAt) <= scope.ToDate.Value));
    }

    /// <summary>Mô tả phạm vi để trả về/thông báo cho người dùng.</summary>
    public static string Describe(PurgeScope scope)
        => scope.IsBySelection
            ? $"chọn {scope.Ids.Count} dòng"
            : $"khoảng ngày {scope.FromDate?.ToString("yyyy-MM-dd") ?? "(không giới hạn)"} → {scope.ToDate?.ToString("yyyy-MM-dd") ?? "(không giới hạn)"}";

    /// <summary>Mốc "từ ngày": chuẩn hoá về UTC, giữ nguyên giờ nếu người dùng có truyền giờ.</summary>
    private static DateTime? NormalizeFrom(DateTime? fromDate)
    {
        if (!fromDate.HasValue)
            return null;

        var value = fromDate.Value;
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    /// <summary>
    /// Mốc "đến ngày": nếu người dùng chỉ gửi NGÀY (00:00) thì lấy hết ngày đó, tránh sót bản ghi bị xoá
    /// trong chính ngày kết thúc.
    /// </summary>
    private static DateTime? NormalizeTo(DateTime? toDate)
    {
        if (!toDate.HasValue)
            return null;

        var value = toDate.Value;
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return utc.TimeOfDay == TimeSpan.Zero
            ? new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc).AddTicks(TimeSpan.TicksPerDay - 1)
            : utc;
    }
}
