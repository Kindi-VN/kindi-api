namespace Kindi.API.Application.DTOs.requests
{
    /// <summary>
    /// Thông tin cá nhân người dùng tự cập nhật ở khu vực thành viên.
    /// </summary>
    public class UpdateMyProfileRequest
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Zalo { get; set; }
    }
}
