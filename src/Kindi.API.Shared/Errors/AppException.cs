namespace Kindi.API.Shared.Errors;

/// <summary>
/// Exception nghiệp vụ — ném kèm <see cref="Error"/> để middleware trả về HTTP status + mã lỗi chuỗi cho UI.
/// Ví dụ: <c>throw new AppException(CollaboratorError.PhoneAlreadyExists.WithParams(phone));</c>
/// </summary>
public class AppException : Exception
{
    public Error Error { get; }

    public AppException(Error error) : base(error.MessageKey)
    {
        Error = error;
    }

    public AppException(Error error, Exception innerException) : base(error.MessageKey, innerException)
    {
        Error = error;
    }
}
