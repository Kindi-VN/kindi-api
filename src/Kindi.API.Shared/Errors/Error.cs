namespace Kindi.API.Shared.Errors;

/// <summary>
/// Lỗi nghiệp vụ của API. <see cref="Status"/> là mã chuỗi UI bắt được (xem <see cref="ErrorStatus"/>),
/// <see cref="MessageKey"/> là key trong SharedResource — dịch theo Accept-Language ở middleware.
/// Message có biến thì truyền qua <see cref="WithParams"/>.
/// </summary>
public sealed record Error(string Status, string MessageKey, object[]? Args = null)
{
    /// <summary>Bản sao của lỗi kèm tham số cho message có placeholder ({0}, {1}...).</summary>
    public Error WithParams(params object[] args) => this with { Args = args };
}
