using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Domain.Interfaces;

/// <summary>
/// DbContext CHỈ ĐỌC — dùng cho mọi truy vấn không tracking (danh sách, chi tiết, tra cứu, thống kê).
///
/// Khác <see cref="IApplicationDbContext"/> (DbContext ghi):
/// - Không tracking entity, không audit, không tự soft-delete; <c>SaveChanges</c> bị chặn.
/// - Có thể trỏ sang connection string đọc riêng (replica) qua <c>ConnectionStrings:ReadConnection</c>.
/// - Không tham gia transaction ghi ⇒ dữ liệu đọc có thể trễ hơn dữ liệu vừa ghi vài trăm ms (replica lag).
///
/// Cần đọc để RỒI CẬP NHẬT chính entity đó thì phải dùng <see cref="IApplicationDbContext"/>.
/// </summary>
public interface IReadDbContext
{
    DbSet<T> Set<T>() where T : class;
}
