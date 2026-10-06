namespace Kindi.API.Application.Common.Helpers;

using Kindi.API.Application.Errors;
using Kindi.API.Domain.Entities;
using Kindi.API.Shared.Errors;

/// <summary>
/// Phạm vi xoá nhật ký: theo DANH SÁCH Id (người dùng tick chọn nhiều dòng) HOẶC theo KHOẢNG NGÀY.
/// Ngày đã chuẩn hoá về UTC (mốc kết thúc phủ hết ngày — xem <see cref="DateRangeBounds"/>).
/// </summary>
public sealed record AuditLogDeleteScope(IReadOnlyList<Guid> Ids, DateTime? FromDate, DateTime? ToDate)
{
    /// <summary>true = xoá theo lựa chọn; false = xoá theo khoảng ngày.</summary>
    public bool IsBySelection => Ids.Count > 0;
}

/// <summary>
/// Quy tắc xoá nhật ký hoạt động — bảng nhật ký là append-only (không có xoá mềm) nên xoá là XOÁ THẬT.
/// Luôn bắt buộc có điều kiện: chọn Id hoặc khoảng ngày, tránh xoá sạch nhật ký do thiếu tham số.
/// </summary>
public static class AuditLogDeletionRules
{
    /// <summary>Số dòng tối đa cho một lần xoá theo lựa chọn (chặn payload quá lớn).</summary>
    public const int MaxIdsPerRequest = 1000;

    /// <summary>
    /// Dựng phạm vi xoá từ yêu cầu: có Id thì xoá theo lựa chọn (bỏ Id rỗng, bỏ trùng); không có Id thì
    /// buộc phải có ít nhất một mốc ngày.
    /// </summary>
    public static AuditLogDeleteScope Resolve(IEnumerable<Guid>? ids, DateTime? fromDate, DateTime? toDate)
    {
        var distinctIds = ids?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (distinctIds.Count > MaxIdsPerRequest)
            throw new AppException(AuditLogError.TooManyIds.WithParams(MaxIdsPerRequest));

        if (distinctIds.Count > 0)
            return new AuditLogDeleteScope(distinctIds, null, null);

        var normalizedFrom = DateRangeBounds.NormalizeFrom(fromDate);
        var normalizedTo = DateRangeBounds.NormalizeTo(toDate);

        if (normalizedFrom == null && normalizedTo == null)
            throw new AppException(AuditLogError.DeleteCriteriaRequired);

        if (normalizedFrom.HasValue && normalizedTo.HasValue && normalizedFrom > normalizedTo)
            throw new AppException(AuditLogError.DeleteRangeInvalid);

        return new AuditLogDeleteScope(Array.Empty<Guid>(), normalizedFrom, normalizedTo);
    }

    /// <summary>Điều kiện xoá áp lên truy vấn nhật ký thao tác dữ liệu.</summary>
    public static IQueryable<AuditLog> Apply(IQueryable<AuditLog> query, AuditLogDeleteScope scope)
        => scope.IsBySelection
            ? query.Where(x => scope.Ids.Contains(x.Id))
            : query.Where(x => (!scope.FromDate.HasValue || x.Timestamp >= scope.FromDate.Value)
                            && (!scope.ToDate.HasValue || x.Timestamp <= scope.ToDate.Value));

    /// <summary>Điều kiện xoá áp lên truy vấn nhật ký xác thực tài khoản.</summary>
    public static IQueryable<AuthAuditLog> Apply(IQueryable<AuthAuditLog> query, AuditLogDeleteScope scope)
        => scope.IsBySelection
            ? query.Where(x => scope.Ids.Contains(x.Id))
            : query.Where(x => (!scope.FromDate.HasValue || x.Timestamp >= scope.FromDate.Value)
                            && (!scope.ToDate.HasValue || x.Timestamp <= scope.ToDate.Value));

    /// <summary>Mô tả phạm vi để ghi lại vết xoá trong nhật ký.</summary>
    public static string Describe(AuditLogDeleteScope scope)
        => scope.IsBySelection
            ? $"chọn {scope.Ids.Count} dòng"
            : $"khoảng ngày {Format(scope.FromDate)} → {Format(scope.ToDate)}";

    private static string Format(DateTime? value)
        => value?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(không giới hạn)";
}
