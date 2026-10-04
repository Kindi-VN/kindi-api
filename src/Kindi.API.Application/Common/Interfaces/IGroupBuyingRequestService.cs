using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IGroupBuyingRequestService
{
	// ===== Công khai / người dùng =====

	/// <summary>Tạo yêu cầu mua chung mới (khách chưa đăng nhập vẫn tạo được, hệ thống tự tạo User).</summary>
	Task<GroupBuyingRequestResponseDto> CreateAsync(CreateGroupBuyingRequestDto request);

	/// <summary>Feed mua chung cho tab "Mua chung" trên trang social: nhóm đã duyệt (Active) + nhóm của chính mình.</summary>
	Task<PagedList<GroupBuyingFeedItemDto>> GetPublicPagedAsync(GetPublicGroupBuyingRequestsQueryDto query);

	/// <summary>Chi tiết công khai: thông tin sản phẩm, tiến độ số người, danh sách người tham gia. Liên hệ chỉ đầy đủ với admin.</summary>
	Task<GroupBuyingDetailDto> GetPublicDetailAsync(Guid id);

	/// <summary>Chi tiết công khai theo mã đơn (dùng cho link chia sẻ, người nhận không cần biết Guid).</summary>
	Task<GroupBuyingDetailDto> GetPublicDetailByCodeAsync(string code);

	/// <summary>Đăng ký tham gia nhóm mua chung (khách được tạo tài khoản + collaborator từ thông tin liên hệ).</summary>
	Task<JoinGroupBuyingResponseDto> JoinAsync(Guid id, JoinGroupBuyingRequestDto request);

	/// <summary>Hủy tham gia nhóm mua chung (người đang đăng nhập).</summary>
	Task<GroupBuyingDetailDto> LeaveAsync(Guid id);

	// ===== Admin =====

	/// <summary>Danh sách phân trang cho admin (xem tất cả) hoặc user (chỉ của mình).</summary>
	Task<PagedList<GroupBuyingRequestResponseDto>> GetPagedAsync(GetGroupBuyingRequestsQueryDto query);

	/// <summary>Chi tiết đầy đủ cho admin (không che thông tin liên hệ).</summary>
	Task<GroupBuyingDetailDto> GetDetailAsync(Guid id);

	/// <summary>Duyệt / đóng / hủy yêu cầu mua chung.</summary>
	Task<GroupBuyingRequestResponseDto> UpdateStatusAsync(Guid id, UpdateGroupBuyingStatusDto request);

	/// <summary>Admin chỉnh sửa thông tin yêu cầu mua chung.</summary>
	Task<GroupBuyingRequestResponseDto> UpdateAsync(Guid id, UpdateGroupBuyingRequestDto request);

	/// <summary>Admin xóa một người khỏi nhóm (không xóa được người mở nhóm).</summary>
	Task<GroupBuyingDetailDto> RemoveParticipantAsync(Guid id, Guid participantId);

	/// <summary>Admin hủy yêu cầu mua chung (xóa mềm).</summary>
	Task DeleteAsync(Guid id);

	/// <summary>Admin khôi phục yêu cầu mua chung đã xoá mềm.</summary>
	Task<GroupBuyingRequestResponseDto> RestoreAsync(Guid id);
}
