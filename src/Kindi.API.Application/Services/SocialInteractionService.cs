using AutoMapper;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Kindi.API.Application.Services;

public class SocialInteractionService : ISocialInteractionService
{
    private readonly IRepository<SocialPost> _postRepo;
    private readonly IRepository<SocialLike> _likeRepo;
    private readonly IRepository<SocialComment> _commentRepo;
    private readonly IRepository<SocialShare> _shareRepo;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SocialInteractionService(
        IRepository<SocialPost> postRepo,
        IRepository<SocialLike> likeRepo,
        IRepository<SocialComment> commentRepo,
        IRepository<SocialShare> shareRepo,
        IStringLocalizer<SharedResource> localizer)
    {
        _postRepo = postRepo;
        _likeRepo = likeRepo;
        _commentRepo = commentRepo;
        _shareRepo = shareRepo;
        _localizer = localizer;
    }

    // ===== LIKE =====
    public async Task ToggleLikeAsync(Guid postId, Guid userId)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null)
            throw new NotFoundException(_localizer["SocialPost_NotFound"]);

        var existingLike = await _likeRepo.GetFirstAsync(l =>
            l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
        {
            _likeRepo.Delete(existingLike);
            post.LikesCount = Math.Max(0, post.LikesCount - 1);
        }
        else
        {
            var like = new SocialLike
            {
                SocialLikeCode = CodeGenerator.Generate("SLK"),
                PostId = postId,
                UserId = userId,
                LikedAt = DateTime.UtcNow
            };
            await _likeRepo.AddAsync(like);
            post.LikesCount++;
        }

        _postRepo.Update(post);
        await _likeRepo.SaveChangesAsync();
    }

    public async Task<bool> HasLikedAsync(Guid postId, Guid userId)
    {
        return await _likeRepo.AnyAsync(l =>
            l.PostId == postId && l.UserId == userId);
    }

    public async Task<int> GetLikeCountAsync(Guid postId)
    {
        return await _likeRepo.CountAsync(l => l.PostId == postId);
    }

    // ===== COMMENT =====
    public async Task<SocialComment> AddCommentAsync(Guid postId, Guid userId, string content, Guid? parentId = null)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null)
            throw new NotFoundException(_localizer["SocialPost_NotFound"]);

        if (parentId.HasValue)
        {
            var parent = await _commentRepo.GetByIdAsync(parentId.Value);
            if (parent == null)
                throw new NotFoundException(_localizer["SocialComment_ParentNotFound"]);
        }

        var comment = new SocialComment
        {
            SocialCommentCode = CodeGenerator.Generate("SCM"),
            PostId = postId,
            UserId = userId,
            Content = content,
            ParentCommentId = parentId,
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepo.AddAsync(comment);
        post.CommentsCount++;
        _postRepo.Update(post);
        await _commentRepo.SaveChangesAsync();

        return comment;
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _commentRepo.GetFirstWithIncludesAsync(
            c => c.Id == commentId,
            q => q.Include(c => c.Post).Include(c => c.User));

        if (comment == null)
            throw new NotFoundException(_localizer["SocialComment_NotFound"]);

        // Chỉ author hoặc post author mới được xóa
        if (comment.UserId != userId && comment.Post.AuthorId != userId)
            throw new UnauthorizedAccessException(_localizer["SocialComment_NoPermission"]);

        _commentRepo.Delete(comment);
        comment.Post.CommentsCount = Math.Max(0, comment.Post.CommentsCount - 1);
        _postRepo.Update(comment.Post);
        await _commentRepo.SaveChangesAsync();
    }

    public async Task<List<SocialComment>> GetCommentsAsync(Guid postId)
    {
        var comments = await _commentRepo.GetListWithIncludesAsync(
            includes: q => q
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User),
            predicate: c => c.PostId == postId && c.ParentCommentId == null,
            orderBy: c => c.CreatedAt,
            isDescending: false);

        return comments.ToList();
    }

    // ===== SHARE =====
    public async Task<SocialShare> SharePostAsync(Guid postId, Guid userId, string? note = null)
    {
        var post = await _postRepo.GetByIdAsync(postId);
        if (post == null)
            throw new NotFoundException(_localizer["SocialPost_NotFound"]);

        var share = new SocialShare
        {
            SocialShareCode = CodeGenerator.Generate("SHR"),
            PostId = postId,
            UserId = userId,
            ShareNote = note,
            SharedAt = DateTime.UtcNow
        };

        await _shareRepo.AddAsync(share);
        post.SharesCount++;
        _postRepo.Update(post);
        await _shareRepo.SaveChangesAsync();

        return share;
    }

    public async Task<int> GetShareCountAsync(Guid postId)
    {
        return await _shareRepo.CountAsync(s => s.PostId == postId);
    }
}