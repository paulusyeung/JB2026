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

    public bool SyncedToCrm { get; set; }
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

public sealed class CrmPersonResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public List<string> Emails { get; set; } = new();

    public List<string> Phones { get; set; } = new();

    public List<string> Companies { get; set; } = new();

    public string JobTitle { get; set; } = string.Empty;

    public string CreatedOn { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    public bool SyncedToCrm { get; set; }
}

public sealed class UpdateCrmPersonRequest
{
    public string Name { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public List<string> Emails { get; set; } = new();

    public List<string> Phones { get; set; } = new();

    public string? CompanyId { get; set; }
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

public sealed class CreateCrmCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public CrmAddress Address { get; set; } = new();
    public string? AccountOwnerId { get; set; }

    /// <summary>
    /// Optional JB2026 customer id. When provided, the customer's metadata is
    /// flagged as synced to Twenty CRM after the company is created.
    /// </summary>
    public Guid? CustomerId { get; set; }
}

public sealed class CrmCompanyCreatedResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}

public sealed class UpdateCrmOpportunityRequest
{
    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string? CloseDate { get; set; }
    public double? Amount { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CompanyId { get; set; }
    public string? PointOfContactId { get; set; }
    public string? OwnerId { get; set; }
}

public sealed class CrmStageOption
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public sealed class CrmTaskRelationResponse
{
    public required string Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class UpdateCrmTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DueDate { get; set; }
    public string? AssigneeId { get; set; }
    public List<string>? RelationIds { get; set; }
}

public sealed class CrmTaskResponse
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string Body { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string DueDate { get; set; } = string.Empty;

    public string Assignee { get; set; } = string.Empty;

    public string AssigneeId { get; set; } = string.Empty;

    public List<CrmTaskRelationResponse> Relations { get; set; } = new();

    public string CreatedOn { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class CrmOpportunityResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Stage { get; set; } = string.Empty;

    public string CloseDate { get; set; } = string.Empty;

    public string Amount { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string CompanyId { get; set; } = string.Empty;

    public string PointOfContact { get; set; } = string.Empty;

    public string PointOfContactId { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public string CreatedOn { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}
