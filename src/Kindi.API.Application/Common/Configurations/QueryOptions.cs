namespace Kindi.API.Application.Common.Configurations;

/// <summary>
/// Cấu hình cho điều kiện lọc "chứa một trong các giá trị" (mục "Query" trong appsettings).
/// Trên PostgreSQL, danh sách giá trị đi xuống DB trong MỘT tham số mảng — tương đương TVP của
/// SQL Server — nên câu SQL gọn, không sinh mỗi giá trị một tham số và tận dụng được index.
/// </summary>
public class QueryOptions
{
    public const string SectionName = "Query";

    /// <summary>Lọc trùng danh sách giá trị trước khi truyền xuống DB.</summary>
    public bool DistinctContainsValues { get; set; } = true;

    /// <summary>
    /// Số giá trị tối đa của một điều kiện chứa (0 = không giới hạn). Danh sách dài hơn sẽ bị cắt bớt
    /// để câu SQL và tham số không phình to ngoài kiểm soát.
    /// </summary>
    public int MaxContainsValues { get; set; } = 1000;

    /// <summary>
    /// Giá trị mặc định dùng khi nơi gọi không truyền cấu hình; nơi nào cần theo appsettings thì
    /// inject <c>IOptions&lt;QueryOptions&gt;</c>.
    /// </summary>
    public static QueryOptions Default { get; } = new();
}
