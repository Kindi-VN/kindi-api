using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Kindi.API.Shared.Constants;

namespace Kindi.API.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class SocialController : ApiControllerBase
{
    private readonly ISocialService _socialService;
    private readonly IStringLocalizer<SharedResource> _stringLocalizer;

    public SocialController(ISocialService socialService, IStringLocalizer<SharedResource> stringLocalizer)
    {
        _socialService = socialService;
        _stringLocalizer = stringLocalizer;
    }

    /// <summary>
    /// Lấy danh sách bài viết (phân trang + filter; mineOnly=true để lấy bài viết của chính mình)
    /// </summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] string? privacy = null,
        [FromQuery] string? tag = null,
        [FromQuery] bool mineOnly = false,
        [FromQuery] bool? isApproved = null)
    {
        var query = new GetPostsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Type = !string.IsNullOrEmpty(type) ? Enum.Parse<PostType>(type, true) : null,
            Privacy = !string.IsNullOrEmpty(privacy) ? Enum.Parse<PrivacyType>(privacy, true) : null,
            Tag = tag,
            MineOnly = mineOnly,
            IsApproved = isApproved
        };

        var result = await _socialService.GetPostsAsync(query);
        return OkPaged(result, _stringLocalizer["Social_GetPostsSuccess"]);
    }

    /// <summary>
    /// Lấy chi tiết bài viết theo ID
    /// </summary>
    [Authorize]
    [AllowAnonymous]
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var result = await _socialService.GetPostByIdAsync(id);
        return Ok(result, _stringLocalizer["Social_GetPostSuccess"]);
    }

    /// <summary>
    /// Tạo bài viết mới
    /// </summary>
    [Authorize]
    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var result = await _socialService.CreatePostAsync(request);
        return Created(nameof(GetPostById), result, _stringLocalizer["Social_CreateSuccess"]);
    }

    /// <summary>
    /// Cập nhật bài viết
    /// </summary>
    [Authorize]
    [HttpPut("posts/{id}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostRequest request)
    {
        var result = await _socialService.UpdatePostAsync(id, request);
        return Ok(result, _stringLocalizer["Social_UpdateSuccess"]);
    }

    /// <summary>
    /// Xóa bài viết
    /// </summary>
    [Authorize]
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        await _socialService.DeletePostAsync(id);
        return Ok(_stringLocalizer["Social_DeleteSuccess"]);
    }

    /// <summary>
    /// Admin: Lấy danh sách bài viết theo trạng thái (approved/pending/deleted/all)
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpGet("posts/admin")]
    public async Task<IActionResult> GetAdminPosts(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _socialService.GetAdminPostsAsync(status, search, fromDate, toDate, pageNumber, pageSize);
        return OkPaged(result, _stringLocalizer["Social_GetPostsSuccess"]);
    }

    /// <summary>
    /// Admin: Lấy danh sách bài viết cần duyệt
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpGet("posts/pending")]
    public async Task<IActionResult> GetPendingPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _socialService.GetPendingPostsAsync(pageNumber, pageSize);
        return OkPaged(result, _stringLocalizer["Social_GetPendingSuccess"]);
    }

    /// <summary>
    /// Admin: Duyệt bài viết
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("posts/{id}/approve")]
    public async Task<IActionResult> ApprovePost(Guid id)
    {
        var result = await _socialService.ApprovePostAsync(id);
        return Ok(result, _stringLocalizer["Social_ApproveSuccess"]);
    }

    /// <summary>
    /// Admin: Từ chối bài viết
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("posts/{id}/reject")]
    public async Task<IActionResult> RejectPost(Guid id, [FromBody] string? reason = null)
    {
        var result = await _socialService.RejectPostAsync(id, reason);
        return Ok(result, _stringLocalizer["Social_RejectSuccess"]);
    }

    /// <summary>
    /// Admin: Khôi phục bài viết đã xóa
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("posts/{id}/restore")]
    public async Task<IActionResult> RestorePost(Guid id)
    {
        var result = await _socialService.RestorePostAsync(id);
        return Ok(result, _stringLocalizer["RestoreSuccess"]);
    }

    /// <summary>
    /// Admin: Ghim bài viết lên đầu trang feed
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("posts/{id}/pin")]
    public async Task<IActionResult> PinPost(Guid id)
    {
        var result = await _socialService.PinPostAsync(id);
        return Ok(result, _stringLocalizer["Social_PinSuccess"]);
    }

    /// <summary>
    /// Admin: Hủy ghim bài viết
    /// </summary>
    [Authorize(Roles = RoleConstants.Admin)]
    [HttpPost("posts/{id}/unpin")]
    public async Task<IActionResult> UnpinPost(Guid id)
    {
        var result = await _socialService.UnpinPostAsync(id);
        return Ok(result, _stringLocalizer["Social_UnpinSuccess"]);
    }
}