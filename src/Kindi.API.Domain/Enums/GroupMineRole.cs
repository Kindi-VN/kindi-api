namespace Kindi.API.Domain.Enums;

/// <summary>Vai trò của người đang đăng nhập với nhóm — dùng cho danh sách "nhóm của tôi".</summary>
public enum GroupMineRole
{
    /// <summary>Nhóm do chính mình tạo</summary>
    Created = 1,

    /// <summary>Nhóm mình đã tham gia</summary>
    Joined = 2
}
