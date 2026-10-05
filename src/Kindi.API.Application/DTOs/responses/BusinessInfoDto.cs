using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Responses;

/// <summary>
/// Thông tin doanh nghiệp — khớp với frontend `BusinessInfo` (shared business-info card).
/// Backend trả nested `businessInfo` trong detail response.
/// </summary>
public class BusinessInfoDto
{
    public string? CompanyName { get; set; }
    public string? CompanyTax { get; set; } = string.Empty;
    public string? CompanyAddress { get; set; }
    public string? CompanyWebsite { get; set; }
    public BusinessType? BusinessType { get; set; }
    public CompanySize? CompanySize { get; set; }
    public string? BusinessField { get; set; }
}
