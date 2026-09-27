namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Hàm dưới DB dùng trong truy vấn EF (không gọi trực tiếp được ở C#).
/// </summary>
public static class KindiDbFunctions
{
    /// <summary>
    /// Bỏ dấu tiếng Việt ngay trong câu truy vấn (extension <c>unaccent</c> của PostgreSQL),
    /// nhờ đó tìm "sua bot" vẫn ra "Sữa bột".
    /// </summary>
    public static string Unaccent(string value)
        => throw new NotSupportedException("Chỉ dùng bên trong câu truy vấn EF.");
}
