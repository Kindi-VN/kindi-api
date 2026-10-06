using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Exceptions;
using Kindi.API.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Linq.Expressions;
using Kindi.API.Application.Common.Extensions;

namespace Kindi.API.Application.Services;

public class SocialService : ISocialService
{
    private readonly IRepository<SocialPost> _postRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<PostTag> _postTagRepository;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISocialInteractionService _interactionService;

    public SocialService(
        IRepository<SocialPost> postRepository,
        IRepository<User> userRepository,
        IRepository<Tag> tagRepository,
        IRepository<PostTag> postTagRepository,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer,
        ICurrentUserService currentUserService,
        ISocialInteractionService interactionService)
    {
        _postRepository = postRepository;
        _userRepository = userRepository;
        _tagRepository = tagRepository;
        _postTagRepository = postTagRepository;
        _mapper = mapper;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _interactionService = interactionService;
    }

    public async Task<PagedList<PostResponse>> GetPostsAsync(GetPostsQuery query)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        Expression<Func<SocialPost, bool>>? predicate = null;

        if (query.Type.HasValue)
            predicate = predicate.And(p => p.Type == query.Type.Value);

        if (!string.IsNullOrEmpty(query.Tag))
            predicate = predicate.And(p => p.PostTags.Any(pt => pt.Tag.Name == query.Tag));

        if (query.IsApproved.HasValue)
            predicate = predicate.And(p => p.IsApproved == query.IsApproved.Value);

        // Lọc theo ngày đăng bài: bỏ trống thì không giới hạn (giữ nguyên hành vi cũ).
        var fromDate = query.FromDate?.Date.ToUniversalTime();
        var toDate = query.ToDate?.Date.AddDays(1).ToUniversalTime();
        if (fromDate.HasValue)
            predicate = predicate.And(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            predicate = predicate.And(p => p.CreatedAt < toDate.Value);

        // Tìm kiếm theo từ khoá: searchField chỉ định thì CHỈ dò đúng một trường;
        // bỏ trống dò trên nội dung bài viết + họ tên/mã tài khoản tác giả.
        var keyword = query.Search?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            Expression<Func<SocialPost, bool>> searchPredicate = query.SearchField switch
            {
                SocialPostSearchField.Content => p => p.Content.Like(keyword),
                SocialPostSearchField.AuthorFullName => p => p.Author.FullName.Like(keyword),
                SocialPostSearchField.AuthorUserCode => p => p.Author.UserCode != null && p.Author.UserCode.EqualsCode(keyword),
                _ => p => p.Content.Like(keyword)
                    || p.Author.FullName.Like(keyword)
                    || (p.Author.UserCode != null && p.Author.UserCode.EqualsCode(keyword))
            };
            predicate = predicate.And(searchPredicate);
        }

        // XÂY DỰNG PREDICATE KHÔNG CÓ AWAIT
        // "Bài viết của tôi" phải có tài khoản: không thì truy vấn rơi về danh sách công khai.
        if (query.MineOnly)
        {
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedException(_localizer["Social_NotAuthorized"]);

            var myGuid = Guid.Parse(userId);
            predicate = predicate.And(p => p.AuthorId == myGuid);
        }
        else if (!isAdmin)
        {
            // Chỉ lấy bài Public đã duyệt + bài của chính user
            // Phần Friends sẽ xử lý sau khi lấy dữ liệu
            if (!string.IsNullOrEmpty(userId))
            {
                var userGuid = Guid.Parse(userId);
                predicate = predicate.And(p =>
                    (p.Privacy == PrivacyType.Public && p.IsApproved == true) ||
                    p.AuthorId == userGuid
                // Friends sẽ xử lý sau
                );
            }
            else
            {
                // Chưa login: chỉ lấy Public đã duyệt
                predicate = predicate.And(p =>
                    p.Privacy == PrivacyType.Public && p.IsApproved == true
                );
            }
        }

        predicate ??= p => true;

        // Lấy dữ liệu
        // Ghim (IsPinned) sempre primește în feed; apoi CreatedAt desc
        // Repository-ul suportă doar un singel orderBy — construim query manual pentru 2 sort keys în SQL
        IQueryable<SocialPost> dbQuery = await _postRepository.GetQueryableAsync();
        // Lọc trước rồi mới include: phần join chỉ chạy trên tập bản ghi còn lại
        if (predicate != null) dbQuery = dbQuery.Where(predicate);
        dbQuery = dbQuery
            .IncludeMultiple(p => p.Author, p => p.Likes, p => p.Comments, p => p.Shares)
            .IncludeThen(p => p.PostTags, pt => pt.Tag);
        dbQuery = dbQuery.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt);

        var posts = await PagedList<SocialPost>.CreateAsync(dbQuery, query.PageNumber, query.PageSize);

        // XỬ LÝ FRIENDS SAU KHI LẤY DỮ LIỆU
        if (!string.IsNullOrEmpty(userId) && !isAdmin)
        {
            var userGuid = Guid.Parse(userId);
            var friendIds = await GetFriendIdsAsync(userGuid); // Lấy danh sách bạn bè

            // Lọc bỏ bài Friends của người không phải bạn bè
            var filteredItems = posts.Items.Where(p =>
                p.Privacy != PrivacyType.Friends ||
                p.AuthorId == userGuid ||
                friendIds.Contains(p.AuthorId)
            ).ToList();

            // Cập nhật lại posts
            posts = new PagedList<SocialPost>(
                filteredItems,
                filteredItems.Count,
                query.PageNumber,
                query.PageSize
            );
        }

        // Map sang response
        var postResponses = _mapper.Map<List<PostResponse>>(posts.Items);

        // Thêm logic lấy Like status cho current user
        if (!string.IsNullOrEmpty(userId))
        {
            var userGuid = Guid.Parse(userId);
            foreach (var response in postResponses)
            {
                var post = posts.Items.First(p => p.Id == response.Id);
                response.IsLiked = await _interactionService.HasLikedAsync(post.Id, userGuid);
            }
        }

        // Gán Author
        foreach (var response in postResponses)
        {
            var post = posts.Items.First(p => p.Id == response.Id);
            response.Author = _mapper.Map<AuthorDto>(post.Author);
        }

        return new PagedList<PostResponse>(
            postResponses,
            posts.TotalCount,
            query.PageNumber,
            query.PageSize
        );
    }

    public async Task<PostResponse> GetPostByIdAsync(Guid id)
    {
        // Thêm Includes cho Likes, Comments, Shares và Comments.User
        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(p => p.Shares)
                    .ThenInclude(s => s.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
        );

        if (post == null)
        {
            throw new NotFoundException(_localizer["Social_NotFound"]);
        }

        var response = _mapper.Map<PostResponse>(post);
        response.Author = _mapper.Map<AuthorDto>(post.Author);

        // Kiểm tra current user đã Like chưa
        var userId = _currentUserService.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            var userGuid = Guid.Parse(userId);
            response.IsLiked = await _interactionService.HasLikedAsync(post.Id, userGuid);
        }

        return response;
    }

    public async Task<PostResponse> CreatePostAsync(CreatePostRequest request)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException(_localizer["Social_NotAuthorized"]);
        }

        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            throw new NotFoundException(_localizer["User_NotFound"]);
        }

        var post = _mapper.Map<SocialPost>(request);
        post.SocialPostCode = CodeGenerator.Generate("SOC");
        post.AuthorId = user.Id;

        // If author is Admin, mark post as approved immediately
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        post.IsApproved = isAdmin ? true : false;
        // No ApprovedAt field on SocialPost entity currently; only mark IsApproved

        // Xử lý Tags
        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tagName in request.Tags.Distinct())
            {
                var tag = await _tagRepository.GetFirstAsync(t => t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { TagCode = CodeGenerator.Generate("TAG"), Name = tagName, UsageCount = 0 };
                    await _tagRepository.AddAsync(tag);
                    await _tagRepository.SaveChangesAsync();
                }
                tag.UsageCount++;

                post.PostTags.Add(new PostTag
                {
                    PostTagCode = CodeGenerator.Generate("PTG"),
                    PostId = post.Id,
                    TagId = tag.Id,
                    Post = post,
                    Tag = tag
                });
            }
        }

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        var createdPost = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == post.Id,
            includes: q => q
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
        ) ?? throw new NotFoundException(_localizer["Social_NotFound"]);

        var response = _mapper.Map<PostResponse>(createdPost);
        response.Author = _mapper.Map<AuthorDto>(createdPost.Author);

        // Thêm message chờ duyệt
        response.Message = _localizer["Social_PendingApproval"];

        return response;
    }

    public async Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request)
    {
        var post = await _postRepository.GetFirstWithIncludesAsync(
        p => p.Id == id,
        includes: q => q
            .Include(p => p.Author)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
        );

        if (post == null)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        if (post.AuthorId.ToString() != userId && !isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        // Nếu chuyển từ private sang public -> cần duyệt lại
        var wasPrivate = post.Privacy != PrivacyType.Public;
        var isNowPublic = request.Privacy == PrivacyType.Public;

        if (wasPrivate && isNowPublic && !isAdmin)
        {
            post.IsApproved = false;
        }

        // Cập nhật Tags
        if (request.Tags != null)
        {
            foreach (var oldPostTag in post.PostTags.ToList())
            {
                var tag = await _tagRepository.GetByIdAsync(oldPostTag.TagId);
                if (tag != null)
                {
                    tag.UsageCount--;
                    _tagRepository.Update(tag);
                }
                _postTagRepository.Delete(oldPostTag);
            }
            post.PostTags.Clear();

            foreach (var tagName in request.Tags.Distinct())
            {
                var tag = await _tagRepository.GetFirstAsync(t => t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { TagCode = CodeGenerator.Generate("TAG"), Name = tagName, UsageCount = 0 };
                    await _tagRepository.AddAsync(tag);
                    await _tagRepository.SaveChangesAsync();
                }
                tag.UsageCount++;
                _tagRepository.Update(tag);

                post.PostTags.Add(new PostTag
                {
                    PostTagCode = CodeGenerator.Generate("PTG"),
                    PostId = post.Id,
                    TagId = tag.Id,
                    Post = post,
                    Tag = tag
                });
            }
        }

        _mapper.Map(request, post);
        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var updatedPost = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
        ) ?? throw new NotFoundException(_localizer["Social_NotFound"]);

        var response = _mapper.Map<PostResponse>(updatedPost);
        response.Author = _mapper.Map<AuthorDto>(updatedPost.Author);
        return response;
    }

    public async Task<bool> DeletePostAsync(Guid id)
    {
        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.PostTags)
        );

        if (post == null)
        {
            throw new NotFoundException(_localizer["Social_NotFound"]);
        }

        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);

        if (post.AuthorId.ToString() != userId && !isAdmin)
        {
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);
        }

        foreach (var postTag in post.PostTags)
        {
            var tag = await _tagRepository.GetByIdAsync(postTag.TagId);
            if (tag != null)
            {
                tag.UsageCount--;
                _tagRepository.Update(tag);
            }
        }

        _postRepository.Delete(post);
        await _postRepository.SaveChangesAsync();
        return true;
    }

    public async Task<PagedList<PostResponse>> GetPendingPostsAsync(int pageNumber, int pageSize)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        Expression<Func<SocialPost, bool>>? predicate = null;
        predicate = predicate.And(p => p.IsApproved == false);
        predicate = predicate.And(p => p.Privacy == PrivacyType.Public);

        var posts = await _postRepository.GetPagedWithIncludesAsync(
            pageNumber,
            pageSize,
            includes: q => q
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag),
            predicate: predicate ?? (p => true),
            orderBy: p => p.CreatedAt,
            isDescending: true
        );

        var postResponses = _mapper.Map<List<PostResponse>>(posts.Items);
        foreach (var response in postResponses)
        {
            var post = posts.Items.First(p => p.Id == response.Id);
            response.Author = _mapper.Map<AuthorDto>(post.Author);
        }

        return new PagedList<PostResponse>(
            postResponses,
            posts.TotalCount,
            pageNumber,
            pageSize
        );
    }

    public async Task<PagedList<PostResponse>> GetAdminPostsAsync(
        string? status,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        IQueryable<SocialPost> dbQuery = await _postRepository.GetQueryableAsync();

        // Tab "Đã xóa": bỏ global filter để lấy các bài soft-delete
        if (string.Equals(status, "deleted", StringComparison.OrdinalIgnoreCase))
        {
            dbQuery = dbQuery.IgnoreQueryFilters().Where(p => p.IsDeleted);
        }
        else if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            dbQuery = dbQuery.Where(p => p.IsApproved);
        }
        else if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            dbQuery = dbQuery.Where(p => !p.IsApproved && p.Privacy == PrivacyType.Public);
        }
        // status = null / "all" => mọi bài chưa xóa

        // Lọc theo tác giả (họ tên / username / mã) hoặc tiêu đề
        var keyword = search?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            dbQuery = dbQuery.Where(p =>
                p.Author.FullName.Like(keyword) ||
                p.Author.Username.Like(keyword) ||
                (p.Author.UserCode != null && p.Author.UserCode.EqualsCode(keyword)) ||
                (p.Title != null && p.Title.Like(keyword)));
        }

        // Lọc theo khoảng ngày tạo
        if (fromDate.HasValue)
            dbQuery = dbQuery.Where(p => p.CreatedAt >= fromDate.Value.Date.ToUniversalTime());
        if (toDate.HasValue)
            dbQuery = dbQuery.Where(p => p.CreatedAt < toDate.Value.Date.AddDays(1).ToUniversalTime());

        dbQuery = dbQuery
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Shares)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.CreatedAt);

        var posts = await PagedList<SocialPost>.CreateAsync(dbQuery, pageNumber, pageSize);

        var postResponses = _mapper.Map<List<PostResponse>>(posts.Items);
        foreach (var response in postResponses)
        {
            var post = posts.Items.First(p => p.Id == response.Id);
            response.Author = _mapper.Map<AuthorDto>(post.Author);
        }

        return new PagedList<PostResponse>(
            postResponses,
            posts.TotalCount,
            pageNumber,
            pageSize
        );
    }

    public async Task<PostResponse> RestorePostAsync(Guid id)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        // GetByIdIncludingDeletedAsync bỏ qua global soft-delete filter → tìm được bài đã xóa
        var post = await _postRepository.GetByIdIncludingDeletedAsync(id);
        if (post == null || !post.IsDeleted)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        post.IsDeleted = false;
        post.UpdatedAt = DateTime.UtcNow;

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var refreshed = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.Author));
        var response = refreshed == null ? _mapper.Map<PostResponse>(post) : _mapper.Map<PostResponse>(refreshed);
        response.Author = refreshed != null
            ? _mapper.Map<AuthorDto>(refreshed.Author)
            : (_mapper.Map<AuthorDto>(post.Author));
        return response;
    }

    public async Task<PostResponse> ApprovePostAsync(Guid id)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.Author)
        );

        if (post == null)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        if (post.IsApproved)
            throw new InvalidOperationException(_localizer["Social_AlreadyApproved"]);

        post.IsApproved = true;

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var response = _mapper.Map<PostResponse>(post);
        response.Author = _mapper.Map<AuthorDto>(post.Author);
        return response;
    }

    public async Task<PostResponse> RejectPostAsync(Guid id, string? reason = null)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.Author)
        );

        if (post == null)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        // Cho phép "hủy duyệt": unapprove bài đã duyệt (hoặc từ chối bài chưa duyệt).
        // Không throw khi đã approved.
        post.IsApproved = false;
        // Có thể thêm field RejectionReason nếu muốn

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var response = _mapper.Map<PostResponse>(post);
        response.Author = _mapper.Map<AuthorDto>(post.Author);
        return response;
    }

    public async Task<PostResponse> PinPostAsync(Guid id)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.Author)
        );

        if (post == null)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        if (post.IsPinned)
            throw new InvalidOperationException(_localizer["Social_AlreadyPinned"]);

        post.IsPinned = true;

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var response = _mapper.Map<PostResponse>(post);
        response.Author = _mapper.Map<AuthorDto>(post.Author);
        return response;
    }

    public async Task<PostResponse> UnpinPostAsync(Guid id)
    {
        var isAdmin = _currentUserService.IsInRole(UserRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(_localizer["Social_NotAuthorized"]);

        var post = await _postRepository.GetFirstWithIncludesAsync(
            p => p.Id == id,
            includes: q => q.Include(p => p.Author)
        );

        if (post == null)
            throw new NotFoundException(_localizer["Social_NotFound"]);

        if (!post.IsPinned)
            throw new InvalidOperationException(_localizer["Social_NotPinned"]);

        post.IsPinned = false;

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync();

        var response = _mapper.Map<PostResponse>(post);
        response.Author = _mapper.Map<AuthorDto>(post.Author);
        return response;
    }

    private async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
    {
        //var friends = await _friendRepository.FindAsync(f =>
        //    (f.UserId == userId || f.FriendId == userId) &&
        //    f.Status == FriendStatus.Accepted
        //);

        //return friends.Select(f => f.UserId == userId ? f.FriendId : f.UserId).ToList();
        return new List<Guid>(); // Trả về danh sách rỗng nếu không có bạn bè
    }
}