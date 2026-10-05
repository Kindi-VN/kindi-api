using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using Kindi.API.Application.Common.Interfaces;

namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Sinh mã tham chiếu tự động theo format "{PREFIX}-{random}".
/// Toàn bộ in hoa, độ dài ≤ 30 ký tự, bỏ các ký tự dễ nhầm (0/O/1/I).
/// </summary>
public static class CodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaxLength = 30;

    /// <summary>
    /// Sinh code dạng "{prefix}-XXXXXX" (in hoa).
    /// </summary>
    public static string Generate(string prefix, int randomLength = 6)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        var cleanPrefix = prefix.Trim().ToUpperInvariant().Replace(" ", "", StringComparison.Ordinal);
        var randomPart = GenerateRandom(randomLength);
        var code = $"{cleanPrefix}-{randomPart}";

        return code.Length <= MaxLength ? code : code[..MaxLength];
    }

    /// <summary>
    /// Sinh code cộng với việc kiểm tra unique theo predicate.
    /// Tối đa 5 lần thử, nếu hết vẫn báo lỗi để tránh vô hạn.
    /// </summary>
    public static async Task<string> GenerateUniqueAsync<T>(
        IQueryService queryService,
        string prefix,
        Expression<Func<T, bool>> codeExists,
        CancellationToken cancellationToken = default) where T : class
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = Generate(prefix);
            if (!await queryService.AnyAsync(codeExists, cancellationToken))
                return code;
        }

        // Rất hiếm xảy ra (36^6 tổ hợp) — tăng độ dài random để thoát khỏi vùng trùng
        return Generate(prefix, randomLength: 10);
    }

    private static string GenerateRandom(int length)
    {
        var buffer = new byte[length];
        RandomNumberGenerator.Fill(buffer);

        var sb = new StringBuilder(length);
        foreach (var b in buffer)
            sb.Append(Chars[b % Chars.Length]);

        return sb.ToString();
    }
}