namespace JB2026.Api.Models;

public sealed class CrmRelationItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class CrmAddress
{
    public string Street1 { get; set; } = string.Empty;
    public string Street2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public sealed class CrmCompanyResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string AccountOwner { get; set; } = string.Empty;

    public string AccountOwnerId { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public CrmAddress Address { get; set; } = new();

    public string FormattedAddress { get; set; } = string.Empty;

    public string CreatedOn { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    public List<CrmRelationItem> People { get; set; } = new();

    public List<CrmRelationItem> Opportunities { get; set; } = new();
}

public sealed class CrmMemberResponse
{
    public required string Id { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class CrmCatalogItem
{
    public required string Id { get; init; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateCrmCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public CrmAddress Address { get; set; } = new();
    public string? AccountOwnerId { get; set; }
    public List<string>? PeopleIds { get; set; }
    public List<string>? OpportunityIds { get; set; }
}
