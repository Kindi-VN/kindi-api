using AutoMapper;
using Kindi.API.Application.Common.Mappings;
using Kindi.API.Domain.Entities;

namespace Kindi.API.Application.DTOs.responses;

public class BusinessGroupCommentResponseDto : IMapFrom<BusinessGroupComment>
{
    public Guid Id { get; set; }
    /// <summary>Mã bình luận hiển thị cho người dùng.</summary>
    public string? BusinessGroupCommentCode { get; set; }
    public Guid BusinessGroupPostId { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
        => profile.CreateMap<BusinessGroupComment, BusinessGroupCommentResponseDto>()
            .ForMember(dest => dest.AuthorName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty));
}
