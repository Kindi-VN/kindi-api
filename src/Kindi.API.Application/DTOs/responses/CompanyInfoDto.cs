using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.Responses;

public class CompanyInfoDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTax { get; set; } = string.Empty;
    public string? CompanyAddress { get; set; }
    public string? CompanyWebsite { get; set; }
    public BusinessType? BusinessType { get; set; }
    public CompanySize? CompanySize { get; set; }
    public string? BusinessField { get; set; }
}
