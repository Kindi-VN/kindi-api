using Kindi.API.Shared.Errors;

namespace Kindi.API.Application.Errors;

/// <summary>Lỗi nghiệp vụ của yêu cầu mua chung.</summary>
public static class GroupBuyingError
{
    public static Error WrongRequest => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_WrongRequest");

    public static Error NotFound => new(ErrorStatus.NotFound, "GroupBuyingRequest_NotFound");

    public static Error InvalidStatus => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_InvalidStatusMessage");

    public static Error CancelReasonRequired => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_CancelReasonRequired");

    public static Error ReasonMaxLength => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_ReasonMaxLength");

    public static Error NotApprovedYet => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_NotApprovedYet");

    public static Error NotOpen => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_NotOpen");

    public static Error ContactRequired => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_ContactRequired");

    public static Error EmailRequired => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_EmailRequired");

    public static Error AlreadyJoined => new(ErrorStatus.Conflict, "GroupBuyingRequest_AlreadyJoined");

    public static Error NotParticipant => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_NotParticipant");

    public static Error ParticipantNotFound => new(ErrorStatus.NotFound, "GroupBuyingRequest_ParticipantNotFound");

    public static Error YouAreCreator => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_YouAreCreator");

    public static Error CreatorCannotJoin => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_CreatorCannotJoin");

    public static Error CreatorCannotLeave => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_CreatorCannotLeave");

    public static Error CannotRemoveCreator => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_CannotRemoveCreator");

    public static Error TargetLessThanCurrent => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_TargetLessThanCurrent");

    public static Error InvalidStatusTransition => new(ErrorStatus.WrongRequest, "GroupBuyingRequest_InvalidStatusTransition");

    public static Error LoginRequired => new(ErrorStatus.Unauthorized, "GroupBuyingRequest_LoginRequired");
}
