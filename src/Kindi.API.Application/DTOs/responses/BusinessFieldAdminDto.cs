namespace Kindi.API.Application.DTOs.Responses;

/// <summary>Lĩnh vực hoạt động cho màn quản trị, kèm số liệu đang dùng.</summary>
public class BusinessFieldAdminDto
{
    public Guid Id { get; set; }
    public string? BusinessFieldCode { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Chuỗi JSON các tên gọi khác, ví dụ ["CNTT","IT"].</summary>
    public string? Aliases { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Số công ty thuộc lĩnh vực.</summary>
    public int CompanyCount { get; set; }

    /// <summary>Số tài khoản thuộc lĩnh vực (CTV + đối tác).</summary>
    public int UserCount { get; set; }
}

/// <summary>Công ty thuộc một lĩnh vực.</summary>
public class BusinessFieldCompanyDto
{
    public Guid Id { get; set; }
    public string? CompanyCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }

    /// <summary>Số hồ sơ cộng tác viên trỏ tới công ty này.</summary>
    public int CollaboratorCount { get; set; }

    /// <summary>Số hồ sơ đối tác trỏ tới công ty này.</summary>
    public int PartnerCount { get; set; }
}

/// <summary>Tài khoản thuộc một lĩnh vực, đi qua hồ sơ CTV hoặc đối tác.</summary>
public class BusinessFieldUserDto
{
    public Guid UserId { get; set; }
    public string? UserCode { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }

    /// <summary>Collaborator hoặc Partner.</summary>
    public string Role { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>Chi tiết một lĩnh vực: công ty và tài khoản thuộc lĩnh vực đó.</summary>
public class BusinessFieldRelatedDto
{
    public Guid Id { get; set; }
    public string? BusinessFieldCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<BusinessFieldCompanyDto> Companies { get; set; } = new();
    public List<BusinessFieldUserDto> Users { get; set; } = new();
}
