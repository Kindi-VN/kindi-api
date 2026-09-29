using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của cộng tác viên.</summary>
public static class CollaboratorError
{
    public static Error WrongRequest => new(ErrorStatus.WrongRequest, "Collaborator_WrongRequest");

    public static Error NotFound => new(ErrorStatus.NotFound, "Collaborator_NotFound");

    public static Error PhoneAlreadyExists => new(ErrorStatus.Conflict, "Collaborator_PhoneAlreadyExists");

    public static Error EmailAlreadyExists => new(ErrorStatus.Conflict, "Collaborator_EmailAlreadyExists");

    public static Error UserAlreadyExists => new(ErrorStatus.Conflict, "Collaborator_UserAlreadyExists");

    public static Error ParentNotFound => new(ErrorStatus.NotFound, "Collaborator_ParentNotFound");

    public static Error ParentNotApproved => new(ErrorStatus.WrongRequest, "Collaborator_ParentNotApproved");

    public static Error LevelExceeded => new(ErrorStatus.WrongRequest, "Collaborator_LevelExceeded");

    public static Error CircularReference => new(ErrorStatus.WrongRequest, "Collaborator_CircularReference");
}
