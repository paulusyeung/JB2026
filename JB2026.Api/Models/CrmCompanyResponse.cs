namespace JB2026.Api.Models;

public sealed class CrmCompanyResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string AccountOwner { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string CreatedOn { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    public List<string> People { get; set; } = new();

    public List<string> Opportunities { get; set; } = new();
}
