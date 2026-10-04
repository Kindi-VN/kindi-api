namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Extensions;
using Xunit;

/// <summary>
/// Chốt quy tắc sắp xếp mặc định của các danh sách: khi client không truyền sortBy/sortOrder thì
/// cột mặc định (thường là CreatedAt) phải sắp giảm dần — bản ghi mới nhất lên đầu; chỉ sắp tăng dần
/// khi client truyền rõ sortOrder = "asc".
/// </summary>
public class PagingSortOrderTests
{
    private sealed class Item
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    private static IQueryable<Item> Items() => new[]
    {
        new Item { Id = 1, Name = "Cũ", CreatedAt = new DateTime(2026, 1, 1) },
        new Item { Id = 2, Name = "Mới", CreatedAt = new DateTime(2026, 3, 1) },
        new Item { Id = 3, Name = "Giữa", CreatedAt = new DateTime(2026, 2, 1) },
    }.AsQueryable();

    [Fact]
    public void Khong_truyen_sort_thi_mac_dinh_moi_nhat_truoc()
    {
        var result = Items().OrderByDynamic(null, null, "CreatedAt").Select(x => x.Id).ToList();

        result.Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public void Truyen_ro_sort_order_asc_thi_sap_tang_dan()
    {
        var result = Items().OrderByDynamic(null, "asc", "CreatedAt").Select(x => x.Id).ToList();

        result.Should().ContainInOrder(1, 3, 2);
    }

    [Fact]
    public void Truyen_ro_sort_order_desc_thi_sap_giam_dan()
    {
        var result = Items().OrderByDynamic(null, "desc", "CreatedAt").Select(x => x.Id).ToList();

        result.Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public void Cot_khong_hop_le_thi_roi_ve_cot_mac_dinh()
    {
        // Tên cột sai sẽ fallback về defaultSortBy, vẫn theo quy tắc mặc định mới nhất trước.
        var result = Items().OrderByDynamic("KhôngTồnTại", null, "CreatedAt").Select(x => x.Id).ToList();

        result.Should().ContainInOrder(2, 3, 1);
    }
}
