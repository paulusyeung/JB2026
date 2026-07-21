using System.Text.Json;
using System.Text.Json.Nodes;
using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services.TwentyCrm;

public class TwentyCrmService : ITwentyCrmService
{
    private readonly IOptions<TwentyCrmOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwentyCrmService> _logger;
    private static bool _opportunityFieldsDiscovered;

    public TwentyCrmService(
        IOptions<TwentyCrmOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TwentyCrmService> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping email lookup for {Email}", email);
            return false;
        }

        try
        {
            const string query = """
                query EmailExists($email: String!) {
                  workspaceMembers(filter: { userEmail: { eq: $email } }) {
                    totalCount
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["email"] = email };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("workspaceMembers", out var members))
                return false;

            var totalCount = members.TryGetProperty("totalCount", out var tc) && tc.ValueKind == JsonValueKind.Number
                ? tc.GetInt32()
                : 0;

            return totalCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email {Email} in Twenty CRM", email);
            return false;
        }
    }

    public async Task<IReadOnlyList<CrmCompanyResponse>> GetCompaniesAsync(
        string? currentUserEmail = null,
        string? lookup = null,
        JB5LegacyReadContext? readContext = null,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty companies list");
            return [];
        }

        try
        {
            var hasLookup = !string.IsNullOrWhiteSpace(lookup);
            var hasEmail = !string.IsNullOrWhiteSpace(currentUserEmail);

            var filterClauses = new List<string>();
            filterClauses.Add(
                hasEmail
                    ? "{ or: [ { accountOwnerId: { is: NULL } }, { accountOwner: { userEmail: { eq: $email } } } ] }"
                    : "{ accountOwnerId: { is: NULL } }");

            if (hasLookup)
                filterClauses.Add("{ name: { ilike: $lookup } }");

            var filterBlock = filterClauses.Count == 1
                ? filterClauses[0]
                : $"{{ and: [ {string.Join(", ", filterClauses)} ] }}";

            var query = $$"""
                query Companies($email: String, $lookup: String, $first: Int) {
                  companies(filter: {{filterBlock}}) {
                    edges {
                      node {
                        id
                        name
                        accountOwnerId
                        domainName {
                          primaryLinkUrl
                        }
                        address {
                          addressStreet1
                          addressStreet2
                          addressCity
                          addressState
                          addressPostcode
                          addressCountry
                        }
                        accountOwner {
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        people {
                          edges {
                            node {
                              id
                              name {
                                firstName
                                lastName
                              }
                            }
                          }
                        }
                        opportunities {
                          edges {
                            node {
                              id
                              name
                            }
                          }
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["email"] = currentUserEmail,
                ["lookup"] = hasLookup ? $"%{lookup}%" : null,
                ["first"] = 200,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("companies", out var companiesEl)
                || !companiesEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<CrmCompanyResponse>();

            var syncedNames = readContext is not null
                ? await GetSyncedToCrmCompanyNamesAsync(readContext, cancellationToken)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var parsed = ParseCompany(node);
                if (parsed is not null)
                {
                    parsed.SyncedToCrm = syncedNames.Contains(parsed.Name);
                    result.Add(parsed);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch companies from Twenty CRM");
            return [];
        }
    }

    private async Task<HashSet<string>> GetSyncedToCrmCompanyNamesAsync(
        JB5LegacyReadContext readContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var customers = await readContext.vwCustomerList_Actives
                .AsNoTracking()
                .GroupJoin(
                    readContext.Customers.AsNoTracking(),
                    view => view.CustomerId,
                    customer => customer.CustomerId,
                    (view, customerGroup) => new { view, customerGroup })
                .SelectMany(
                    x => x.customerGroup.DefaultIfEmpty(),
                    (x, customer) => new
                    {
                        x.view.CustomerName,
                        MetadataXml = customer != null ? customer.MetadataXml : null,
                    })
                .Where(row => !string.IsNullOrWhiteSpace(row.CustomerName))
                .ToListAsync(cancellationToken);

            var syncedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var customer in customers)
            {
                if (TryGetMetadataCode(customer.MetadataXml, "SyncedToCRM") == "1"
                    && !string.IsNullOrWhiteSpace(customer.CustomerName))
                {
                    syncedNames.Add(customer.CustomerName!);
                }
            }

            return syncedNames;
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string TryGetMetadataCode(string? metadataXml, string key)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(metadataXml.Trim());
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Fall back to empty when metadata is not valid JSON.
        }

        return string.Empty;
    }

    public async Task<HashSet<string>> GetAllCompanyNamesAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty company name set");
            return [];
        }

        try
        {
            const string query = """
                query AllCompanyNames($first: Int) {
                  companies {
                    edges {
                      node {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["first"] = 5000 };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("companies", out var companiesEl)
                || !companiesEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var name = GetStringProp(node, "name");
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }

            return names;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch company names from Twenty CRM");
            return [];
        }
    }

    public async Task<CrmCompanyResponse?> GetCompanyByIdAsync(
        string id,
        JB5LegacyReadContext? readContext = null,
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning null for company {Id}", id);
            return null;
        }

        try
        {
            const string query = """
                query GetCompany($id: ID!) {
                  companies(filter: { id: { eq: $id } }) {
                    edges {
                      node {
                        id
                        name
                        accountOwnerId
                        domainName {
                          primaryLinkUrl
                        }
                        address {
                          addressStreet1
                          addressStreet2
                          addressCity
                          addressState
                          addressPostcode
                          addressCountry
                        }
                        accountOwner {
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        people {
                          edges {
                            node {
                              id
                              name {
                                firstName
                                lastName
                              }
                            }
                          }
                        }
                        opportunities {
                          edges {
                            node {
                              id
                              name
                            }
                          }
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["id"] = id };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("companies", out var companiesEl)
                || !companiesEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array
                || edges.GetArrayLength() == 0)
                return null;

            var firstEdge = edges[0];
            if (!firstEdge.TryGetProperty("node", out var companyEl) || companyEl.ValueKind != JsonValueKind.Object)
                return null;

            var company = ParseCompany(companyEl);
            if (company is not null && readContext is not null)
            {
                var syncedNames = await GetSyncedToCrmCompanyNamesAsync(readContext, cancellationToken);
                company.SyncedToCrm = syncedNames.Contains(company.Name);
            }

            return company;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch company {Id} from Twenty CRM", id);
            return null;
        }
    }

    public async Task<CrmCompanyResponse?> UpdateCompanyAsync(string id, UpdateCrmCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping update for company {Id}", id);
            return null;
        }

        try
        {
            var updateData = new Dictionary<string, object?>
            {
                ["name"] = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            };

            if (!string.IsNullOrWhiteSpace(request.DomainName))
            {
                updateData["domainName"] = new Dictionary<string, object?>
                {
                    ["primaryLinkUrl"] = NormalizeDomainUrl(request.DomainName.Trim()),
                };
            }

            updateData["address"] = new Dictionary<string, object?>
            {
                ["addressStreet1"] = string.IsNullOrWhiteSpace(request.Address.Street1) ? null : request.Address.Street1.Trim(),
                ["addressStreet2"] = string.IsNullOrWhiteSpace(request.Address.Street2) ? null : request.Address.Street2.Trim(),
                ["addressCity"] = string.IsNullOrWhiteSpace(request.Address.City) ? null : request.Address.City.Trim(),
                ["addressState"] = string.IsNullOrWhiteSpace(request.Address.State) ? null : request.Address.State.Trim(),
                ["addressPostcode"] = string.IsNullOrWhiteSpace(request.Address.Postcode) ? null : request.Address.Postcode.Trim(),
                ["addressCountry"] = string.IsNullOrWhiteSpace(request.Address.Country) ? null : request.Address.Country.Trim(),
            };

            updateData["accountOwnerId"] = string.IsNullOrWhiteSpace(request.AccountOwnerId) ? null : request.AccountOwnerId;

            var query = """
                query GetCurrentRelations($id: ID!) {
                  companies(filter: { id: { eq: $id } }) {
                    edges {
                      node {
                        id
                        people {
                          edges { node { id } }
                        }
                        opportunities {
                          edges { node { id } }
                        }
                      }
                    }
                  }
                }
                """;

            var currentVars = new Dictionary<string, object?> { ["id"] = id };
            var currentData = await PostGraphQLAsync(query, currentVars, cancellationToken);

            var currentPeopleIds = new List<string>();
            var currentOpportunityIds = new List<string>();

            if (currentData.TryGetProperty("companies", out var companiesEl)
                && companiesEl.TryGetProperty("edges", out var curEdges)
                && curEdges.ValueKind == JsonValueKind.Array
                && curEdges.GetArrayLength() > 0
                && curEdges[0].TryGetProperty("node", out var curNode))
            {
                currentPeopleIds = ExtractRelationIds(curNode, "people");
                currentOpportunityIds = ExtractRelationIds(curNode, "opportunities");
            }

            if (request.PeopleIds is not null)
                await SyncCompanyRelationAsync(id, "person", currentPeopleIds, request.PeopleIds, cancellationToken);

            if (request.OpportunityIds is not null)
                await SyncCompanyRelationAsync(id, "opportunity", currentOpportunityIds, request.OpportunityIds, cancellationToken);

            query = """
                mutation UpdateCompany($id: ID!, $data: CompanyUpdateInput!) {
                  updateCompany(id: $id, data: $data) {
                    id
                    name
                    accountOwnerId
                    domainName {
                      primaryLinkUrl
                    }
                    address {
                      addressStreet1
                      addressStreet2
                      addressCity
                      addressState
                      addressPostcode
                      addressCountry
                    }
                    accountOwner {
                      name {
                        firstName
                        lastName
                      }
                      userEmail
                    }
                    people {
                      edges {
                        node {
                          id
                          name {
                            firstName
                            lastName
                          }
                        }
                      }
                    }
                    opportunities {
                      edges {
                        node {
                          id
                          name
                        }
                      }
                    }
                    createdAt
                    createdBy {
                      name
                    }
                    updatedAt
                    updatedBy {
                      name
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["data"] = updateData,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("updateCompany", out var companyEl) || companyEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParseCompany(companyEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update company {Id} in Twenty CRM", id);
            throw;
        }
    }

    public async Task<CrmCompanyCreatedResponse?> CreateCompanyAsync(CreateCrmCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping create company");
            return null;
        }

        try
        {
            var createData = new Dictionary<string, object?>
            {
                ["name"] = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            };

            if (!string.IsNullOrWhiteSpace(request.DomainName))
            {
                createData["domainName"] = new Dictionary<string, object?>
                {
                    ["primaryLinkUrl"] = NormalizeDomainUrl(request.DomainName.Trim()),
                };
            }

            createData["address"] = new Dictionary<string, object?>
            {
                ["addressStreet1"] = string.IsNullOrWhiteSpace(request.Address.Street1) ? null : request.Address.Street1.Trim(),
                ["addressStreet2"] = string.IsNullOrWhiteSpace(request.Address.Street2) ? null : request.Address.Street2.Trim(),
                ["addressCity"] = string.IsNullOrWhiteSpace(request.Address.City) ? null : request.Address.City.Trim(),
                ["addressState"] = string.IsNullOrWhiteSpace(request.Address.State) ? null : request.Address.State.Trim(),
                ["addressPostcode"] = string.IsNullOrWhiteSpace(request.Address.Postcode) ? null : request.Address.Postcode.Trim(),
                ["addressCountry"] = string.IsNullOrWhiteSpace(request.Address.Country) ? null : request.Address.Country.Trim(),
            };

            if (!string.IsNullOrWhiteSpace(request.AccountOwnerId))
                createData["accountOwnerId"] = request.AccountOwnerId;

            const string query = """
                mutation CreateCompany($data: CompanyCreateInput!) {
                  createCompany(data: $data) {
                    id
                    name
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["data"] = createData,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("createCompany", out var companyEl) || companyEl.ValueKind != JsonValueKind.Object)
                return null;

            var id = GetStringProp(companyEl, "id");
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return new CrmCompanyCreatedResponse
            {
                Id = id,
                Name = GetStringProp(companyEl, "name"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create company in Twenty CRM");
            throw;
        }
    }

    public async Task<IReadOnlyList<CrmMemberResponse>> GetWorkspaceMembersAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty member list");
            return [];
        }

        try
        {
            const string query = """
                query GetMembers {
                  workspaceMembers {
                    edges {
                      node {
                        id
                        userEmail
                        name {
                          firstName
                          lastName
                        }
                      }
                    }
                  }
                }
                """;

            var data = await PostGraphQLAsync(query, [], cancellationToken);

            if (!data.TryGetProperty("workspaceMembers", out var membersEl)
                || !membersEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<CrmMemberResponse>();

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var id = GetStringProp(node, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var email = GetStringProp(node, "userEmail");
                var displayName = GetCompositeName(node, "name");
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = email;

                result.Add(new CrmMemberResponse
                {
                    Id = id,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
                    Email = email,
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch workspace members from Twenty CRM");
            return [];
        }
    }

    public async Task<IReadOnlyList<CrmPersonResponse>> GetPeopleAsync(string? lookup = null, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty people list");
            return [];
        }

        try
        {
            var hasLookup = !string.IsNullOrWhiteSpace(lookup);

            var query = $$"""
                query People($lookup: String, $first: Int) {
                  people(filter: {{(hasLookup ? "{ name: { ilike: $lookup } }" : "{}")}}) {
                    edges {
                      node {
                        id
                        name {
                          firstName
                          lastName
                        }
                        emails {
                          primaryEmail
                          additionalEmails
                        }
                        phones {
                          primaryPhoneNumber
                          primaryPhoneCallingCode
                          additionalPhones
                        }
                        jobTitle
                        company {
                          id
                          name
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["lookup"] = hasLookup ? $"%{lookup}%" : null,
                ["first"] = 200,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("people", out var peopleEl)
                || !peopleEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<CrmPersonResponse>();

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var person = ParsePerson(node);
                if (person is not null)
                    result.Add(person);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch people from Twenty CRM");
            return [];
        }
    }

    private CrmPersonResponse? ParsePerson(JsonElement node)
    {
        var personId = GetStringProp(node, "id");
        if (string.IsNullOrWhiteSpace(personId))
            return null;

        return new CrmPersonResponse
        {
            Id = personId,
            Name = GetCompositeName(node, "name"),
            Emails = ResolveEmails(node),
            Phones = ResolvePhones(node),
            Companies = ResolveCompanies(node),
            JobTitle = GetStringProp(node, "jobTitle"),
            CreatedOn = GetStringProp(node, "createdAt"),
            CreatedBy = ResolveActorName(node, "createdBy"),
            UpdatedOn = GetStringProp(node, "updatedAt"),
            UpdatedBy = ResolveActorName(node, "updatedBy"),
        };
    }

    private static List<string> ResolveEmails(JsonElement person)
    {
        var emails = new List<string>();

        if (!person.TryGetProperty("emails", out var emailsEl) || emailsEl.ValueKind != JsonValueKind.Object)
            return emails;

        var primary = GetStringProp(emailsEl, "primaryEmail");
        if (!string.IsNullOrWhiteSpace(primary))
            emails.Add(primary);

        if (emailsEl.TryGetProperty("additionalEmails", out var additional) && additional.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in additional.EnumerateArray())
            {
                var value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                if (!string.IsNullOrWhiteSpace(value) && !emails.Contains(value))
                    emails.Add(value);
            }
        }

        return emails;
    }

    private static List<string> ResolvePhones(JsonElement person)
    {
        var phones = new List<string>();

        if (!person.TryGetProperty("phones", out var phonesEl) || phonesEl.ValueKind != JsonValueKind.Object)
            return phones;

        var callingCode = GetStringProp(phonesEl, "primaryPhoneCallingCode");

        var primary = GetStringProp(phonesEl, "primaryPhoneNumber");
        var primaryCombined = CombinePhone(callingCode, primary);
        if (!string.IsNullOrWhiteSpace(primaryCombined))
            phones.Add(primaryCombined);

        if (phonesEl.TryGetProperty("additionalPhones", out var additional) && additional.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in additional.EnumerateArray())
            {
                string? number = null;
                string? itemCallingCode = callingCode;

                if (item.ValueKind == JsonValueKind.String)
                    number = item.GetString();
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    number = GetStringProp(item, "number") ?? GetStringProp(item, "phoneNumber");
                    var itemCode = GetStringProp(item, "callingCode") ?? GetStringProp(item, "primaryPhoneCallingCode");
                    if (!string.IsNullOrWhiteSpace(itemCode))
                        itemCallingCode = itemCode;
                }

                var combined = CombinePhone(itemCallingCode, number);
                if (!string.IsNullOrWhiteSpace(combined) && !phones.Contains(combined))
                    phones.Add(combined);
            }
        }

        return phones;
    }

    private static string CombinePhone(string callingCode, string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return string.Empty;

        // Number already carries an explicit E.164 calling code — return as-is
        // to avoid doubling it with the separate callingCode field.
        if (number!.StartsWith("+", StringComparison.Ordinal))
            return number;

        if (string.IsNullOrWhiteSpace(callingCode))
            return number;

        return $"{callingCode} {number}";
    }

    // Supported international calling codes (digits), longest first, used to
    // split an E.164 number into its calling code and national number so the
    // value can be stored in Twenty's native separate fields.
    private static readonly string[] CallingCodePrefixes =
    [
        "852", "886", "65", "81", "61", "44", "86", "1",
    ];

    private static (string callingCode, string nationalNumber) SplitE164(string e164)
    {
        var trimmed = (e164 ?? string.Empty).Trim();
        if (!trimmed.StartsWith("+", StringComparison.Ordinal))
            return (string.Empty, trimmed);

        var digits = trimmed[1..];
        foreach (var prefix in CallingCodePrefixes)
        {
            if (digits.StartsWith(prefix, StringComparison.Ordinal))
                return ($"+{prefix}", digits[prefix.Length..]);
        }

        return (string.Empty, trimmed);
    }

    private static (string firstName, string lastName) SplitPersonName(string? fullName)
    {
        var trimmed = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return (string.Empty, string.Empty);

        var parts = trimmed.Split(' ', 2);
        if (parts.Length == 1)
            return (parts[0], string.Empty);

        return (parts[0], parts[1].Trim());
    }

    private static string ToE164(string? rawPhone)
    {
        var trimmed = (rawPhone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        // Already in E.164 form (e.g. +1123456789) — return as-is.
        if (trimmed.StartsWith("+", StringComparison.Ordinal) && !trimmed.Contains(' '))
            return trimmed;

        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex > 0 && trimmed[0] == '+')
        {
            var code = trimmed[..spaceIndex].Trim();
            var number = trimmed[(spaceIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(number))
                return $"{code}{number}";
        }

        return trimmed;
    }

    private static List<string> ResolveCompanies(JsonElement person)
    {
        var companies = new List<string>();

        if (person.TryGetProperty("company", out var company) && company.ValueKind == JsonValueKind.Object)
        {
            var name = GetStringProp(company, "name");
            if (!string.IsNullOrWhiteSpace(name))
                companies.Add(name);
        }

        if (person.TryGetProperty("companies", out var companiesEl) && companiesEl.ValueKind == JsonValueKind.Object)
        {
            if (companiesEl.TryGetProperty("edges", out var edges) && edges.ValueKind == JsonValueKind.Array)
            {
                foreach (var edge in edges.EnumerateArray())
                {
                    if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                        continue;

                    var name = GetStringProp(node, "name");
                    if (!string.IsNullOrWhiteSpace(name) && !companies.Contains(name))
                        companies.Add(name);
                }
            }
        }

        return companies;
    }

    public async Task<CrmPersonResponse?> UpdatePersonAsync(string id, UpdateCrmPersonRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping update for person {Id}", id);
            return null;
        }

        try
        {
            var primaryEmail = request.Emails.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))?.Trim();
            var additionalEmails = request.Emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Skip(primaryEmail is null ? 0 : 1)
                .Select(e => e!.Trim())
                .ToList();

            var primaryPhoneRaw = request.Phones.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))?.Trim();
            var primaryE164 = ToE164(primaryPhoneRaw);
            var (primaryCallingCode, primaryNationalNumber) = SplitE164(primaryE164);
            var additionalPhones = request.Phones
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Skip(primaryPhoneRaw is null ? 0 : 1)
                .Select(p => ToE164(p!.Trim()))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(e164 =>
                {
                    var (code, national) = SplitE164(e164);
                    return (object)new Dictionary<string, object?>
                    {
                        ["callingCode"] = string.IsNullOrWhiteSpace(code) ? primaryCallingCode : code,
                        ["number"] = national,
                    };
                })
                .ToList();

            var (firstName, lastName) = SplitPersonName(request.Name);

            var updateData = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?>
                {
                    ["firstName"] = firstName,
                    ["lastName"] = lastName,
                },
                ["jobTitle"] = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
                ["emails"] = new Dictionary<string, object?>
                {
                    ["primaryEmail"] = primaryEmail,
                    ["additionalEmails"] = additionalEmails,
                },
                ["phones"] = new Dictionary<string, object?>
                {
                    ["primaryPhoneNumber"] = primaryNationalNumber,
                    ["primaryPhoneCallingCode"] = primaryCallingCode,
                    ["additionalPhones"] = additionalPhones,
                },
                ["companyId"] = string.IsNullOrWhiteSpace(request.CompanyId) ? null : request.CompanyId,
            };

            const string query = """
                mutation UpdatePerson($id: ID!, $data: PersonUpdateInput!) {
                  updatePerson(id: $id, data: $data) {
                    id
                    name {
                      firstName
                      lastName
                    }
                    emails {
                      primaryEmail
                      additionalEmails
                    }
                    phones {
                      primaryPhoneNumber
                      primaryPhoneCallingCode
                      additionalPhones
                    }
                    jobTitle
                    company {
                      id
                      name
                    }
                    createdAt
                    createdBy {
                      name
                    }
                    updatedAt
                    updatedBy {
                      name
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["data"] = updateData,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("updatePerson", out var personEl) || personEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParsePerson(personEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update person {Id} in Twenty CRM", id);
            throw;
        }
    }

    public async Task<CrmPersonResponse?> CreatePersonAsync(UpdateCrmPersonRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — skipping create person");
            return null;
        }

        try
        {
            var primaryEmail = request.Emails.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e))?.Trim();
            var additionalEmails = request.Emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Skip(primaryEmail is null ? 0 : 1)
                .Select(e => e!.Trim())
                .ToList();

            var primaryPhoneRaw = request.Phones.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p))?.Trim();
            var primaryE164 = ToE164(primaryPhoneRaw);
            var (primaryCallingCode, primaryNationalNumber) = SplitE164(primaryE164);
            var additionalPhones = request.Phones
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Skip(primaryPhoneRaw is null ? 0 : 1)
                .Select(p => ToE164(p!.Trim()))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(e164 =>
                {
                    var (code, national) = SplitE164(e164);
                    return (object)new Dictionary<string, object?>
                    {
                        ["callingCode"] = string.IsNullOrWhiteSpace(code) ? primaryCallingCode : code,
                        ["number"] = national,
                    };
                })
                .ToList();

            var (firstName, lastName) = SplitPersonName(request.Name);

            var createData = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?>
                {
                    ["firstName"] = firstName,
                    ["lastName"] = lastName,
                },
                ["jobTitle"] = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
                ["emails"] = new Dictionary<string, object?>
                {
                    ["primaryEmail"] = primaryEmail,
                    ["additionalEmails"] = additionalEmails,
                },
                ["phones"] = new Dictionary<string, object?>
                {
                    ["primaryPhoneNumber"] = primaryNationalNumber,
                    ["primaryPhoneCallingCode"] = primaryCallingCode,
                    ["additionalPhones"] = additionalPhones,
                },
                ["companyId"] = string.IsNullOrWhiteSpace(request.CompanyId) ? null : request.CompanyId,
            };

            const string query = """
                mutation CreatePerson($data: PersonCreateInput!) {
                  createPerson(data: $data) {
                    id
                    name {
                      firstName
                      lastName
                    }
                    emails {
                      primaryEmail
                      additionalEmails
                    }
                    phones {
                      primaryPhoneNumber
                      primaryPhoneCallingCode
                      additionalPhones
                    }
                    jobTitle
                    company {
                      id
                      name
                    }
                    createdAt
                    createdBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                    updatedAt
                    updatedBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["data"] = createData,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("createPerson", out var personEl) || personEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParsePerson(personEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create person in Twenty CRM");
            throw;
        }
    }

    public async Task<IReadOnlyList<CrmOpportunityResponse>> GetOpportunitiesAsync(string? lookup = null, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty opportunities list");
            return [];
        }

        try
        {
            // Discover Opportunity type fields once to find custom field names
            await DiscoverOpportunityFieldsAsync(cancellationToken);

            var hasLookup = !string.IsNullOrWhiteSpace(lookup);

            var query = $$"""
                query Opportunities($lookup: String, $first: Int) {
                  opportunities(filter: {{(hasLookup ? "{ name: { ilike: $lookup } }" : "{}")}}) {
                    edges {
                      node {
                        id
                        name
                        stage
                        closeDate
                        amount {
                          amountMicros
                          currencyCode
                        }
                        company {
                          id
                          name
                        }
                        pointOfContact {
                          id
                          name {
                            firstName
                            lastName
                          }
                        }
                        owner {
                          id
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["lookup"] = hasLookup ? $"%{lookup}%" : null,
                ["first"] = 200,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("opportunities", out var opportunitiesEl)
                || !opportunitiesEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("No opportunities data returned from Twenty CRM GraphQL");
                return [];
            }

            var result = new List<CrmOpportunityResponse>();

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var parsed = ParseOpportunity(node);
                if (parsed is not null)
                {
                    if (string.IsNullOrWhiteSpace(parsed.Amount) && !string.IsNullOrWhiteSpace(parsed.Name))
                    {
                        var keys = string.Join(", ", node.EnumerateObject().Select(p => $"{p.Name}({p.Value.ValueKind})"));
                        _logger.LogWarning(
                            "Opportunity '{Name}' ({Id}) has empty amount. Available fields: {Keys}",
                            parsed.Name, parsed.Id, keys);
                    }

                    result.Add(parsed);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch opportunities from Twenty CRM (lookup: {Lookup})", lookup);
            return [];
        }
    }

    private CrmOpportunityResponse? ParseOpportunity(JsonElement node)
    {
        var oppId = GetStringProp(node, "id");
        if (string.IsNullOrWhiteSpace(oppId))
            return null;

        var amount = ResolveAmount(node);
        var currencyCode = ResolveCurrencyCode(node);

        var company = string.Empty;
        var companyId = string.Empty;
        if (node.TryGetProperty("company", out var companyEl) && companyEl.ValueKind == JsonValueKind.Object)
        {
            company = GetStringProp(companyEl, "name");
            companyId = GetStringProp(companyEl, "id");
        }

        return new CrmOpportunityResponse
        {
            Id = oppId,
            Name = GetStringProp(node, "name"),
            Stage = GetStringProp(node, "stage"),
            CloseDate = GetStringProp(node, "closeDate"),
            Amount = amount,
            CurrencyCode = currencyCode,
            Company = company,
            CompanyId = companyId,
            PointOfContact = ResolveOpportunityContact(node),
            PointOfContactId = ResolveOpportunityContactId(node),
            Owner = ResolveOpportunityOwner(node),
            OwnerId = ResolveOpportunityOwnerId(node),
            CreatedOn = GetStringProp(node, "createdAt"),
            CreatedBy = ResolveActorName(node, "createdBy"),
            UpdatedOn = GetStringProp(node, "updatedAt"),
            UpdatedBy = ResolveActorName(node, "updatedBy"),
        };
    }

    private static string ResolveAmount(JsonElement node)
    {
        if (!node.TryGetProperty("amount", out var amtEl) || amtEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var micros = amtEl.TryGetProperty("amountMicros", out var microsEl) && microsEl.ValueKind == JsonValueKind.Number
            ? microsEl.GetRawText()
            : null;

        var currency = GetStringProp(amtEl, "currencyCode");

        if (micros is null)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(currency))
            return FormatAmountFromMicros(micros);

        return $"{currency} {FormatAmountFromMicros(micros)}";
    }

    private static string ResolveCurrencyCode(JsonElement node)
    {
        if (!node.TryGetProperty("amount", out var amtEl) || amtEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetStringProp(amtEl, "currencyCode") ?? string.Empty;
    }

    private static string FormatAmountFromMicros(string microsRaw)
    {
        if (string.IsNullOrWhiteSpace(microsRaw))
            return string.Empty;

        if (decimal.TryParse(microsRaw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            var mainUnit = value / 1_000_000m;
            return mainUnit.ToString("#,##0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        return microsRaw;
    }



    private static string ResolveOpportunityContact(JsonElement opportunity)
    {
        if (!opportunity.TryGetProperty("pointOfContact", out var contact) || contact.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetCompositeName(contact, "name");
    }

    private static string ResolveOpportunityOwner(JsonElement opportunity)
    {
        if (!opportunity.TryGetProperty("owner", out var owner) || owner.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var name = GetCompositeName(owner, "name");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return GetStringProp(owner, "userEmail");
    }

    private static string ResolveOpportunityContactId(JsonElement opportunity)
    {
        if (!opportunity.TryGetProperty("pointOfContact", out var contact) || contact.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetStringProp(contact, "id") ?? string.Empty;
    }

    private static string ResolveOpportunityOwnerId(JsonElement opportunity)
    {
        if (!opportunity.TryGetProperty("owner", out var owner) || owner.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetStringProp(owner, "id") ?? string.Empty;
    }

    public async Task<IReadOnlyList<CrmStageOption>> GetOpportunityStageOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string query = """
                {
                  __type(name: "OpportunityStageEnum") {
                    enumValues {
                      name
                      description
                    }
                  }
                }
                """;

            var data = await PostGraphQLAsync(query, new Dictionary<string, object?>(), cancellationToken);

            if (data.TryGetProperty("__type", out var typeEl)
                && typeEl.TryGetProperty("enumValues", out var vals)
                && vals.ValueKind == JsonValueKind.Array)
            {
                return vals.EnumerateArray()
                    .Select(v => new CrmStageOption
                    {
                        Value = GetStringProp(v, "name"),
                        Label = GetStringProp(v, "description") ?? HumanizeEnumName(GetStringProp(v, "name")),
                    })
                    .Where(o => !string.IsNullOrWhiteSpace(o.Value))
                    .ToList();
            }

            return Array.Empty<CrmStageOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch OpportunityStageEnum values");
            return Array.Empty<CrmStageOption>();
        }
    }

    private static string HumanizeEnumName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return string.Join(" ", name.Split('_')
            .Select(word => word.Length > 0
                ? char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()
                : word));
    }

    private async Task DiscoverOpportunityFieldsAsync(CancellationToken cancellationToken)
    {
        if (_opportunityFieldsDiscovered)
            return;

        _opportunityFieldsDiscovered = true;

        try
        {
            const string query = """
                {
                  __type(name: "Opportunity") {
                    fields {
                      name
                      type {
                        name
                        kind
                        ofType {
                          name
                          kind
                          enumValues {
                            name
                          }
                        }
                        fields {
                          name
                          type {
                            name
                            kind
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var data = await PostGraphQLAsync(query, new Dictionary<string, object?>(), cancellationToken);

            if (data.TryGetProperty("__type", out var typeEl)
                && typeEl.TryGetProperty("fields", out var fieldsEl)
                && fieldsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in fieldsEl.EnumerateArray())
                {
                    var fName = GetStringProp(f, "name");
                    if (fName != "stage" && fName != "amount" && fName != "company" && fName != "pointOfContact" && fName != "owner")
                        continue;

                    if (!f.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.Object)
                        continue;

                    var typeName = GetStringProp(t, "name");
                    var typeKind = GetStringProp(t, "kind");

                    // Check for enum values via ofType
                    if (t.TryGetProperty("ofType", out var ofType) && ofType.ValueKind == JsonValueKind.Object)
                    {
                        var innerName = GetStringProp(ofType, "name");
                        var innerKind = GetStringProp(ofType, "kind");
                        if (ofType.TryGetProperty("enumValues", out var enumVals) && enumVals.ValueKind == JsonValueKind.Array)
                        {
                            var vals = enumVals.EnumerateArray()
                                .Select(v => GetStringProp(v, "name"))
                                .Where(v => !string.IsNullOrWhiteSpace(v))
                                .ToList();
                            _logger.LogWarning("Twenty CRM {Field} enum values ({InnerKind}: {InnerName}): {Values}",
                                fName, innerKind, innerName, string.Join(", ", vals));
                        }
                        else
                        {
                            _logger.LogWarning("Twenty CRM {Field}({TypeKind}: {TypeName}) -> ofType({InnerKind}: {InnerName})",
                                fName, typeKind, typeName, innerKind, innerName);
                        }
                    }

                    if (t.TryGetProperty("fields", out var subFields) && subFields.ValueKind == JsonValueKind.Array)
                    {
                        var subFieldList = subFields.EnumerateArray()
                            .Select(sf =>
                            {
                                var sfName = GetStringProp(sf, "name");
                                var sfType = sf.TryGetProperty("type", out var sft) ? GetStringProp(sft, "name") : "?";
                                return $"{sfName}: {sfType}";
                            })
                            .ToList();

                        _logger.LogWarning("Twenty CRM {Field}({TypeKind}: {TypeName}) sub-fields: {SubFields}",
                            fName, typeKind, typeName, string.Join(", ", subFieldList));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover Opportunity type fields via introspection");
        }
    }

    public async Task<CrmOpportunityResponse?> GetOpportunityByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning null for opportunity {Id}", id);
            return null;
        }

        try
        {
            const string query = """
                query GetOpportunity($id: ID!) {
                  opportunities(filter: { id: { eq: $id } }) {
                    edges {
                      node {
                        id
                        name
                        stage
                        closeDate
                        amount {
                          amountMicros
                          currencyCode
                        }
                        company {
                          id
                          name
                        }
                        pointOfContact {
                          id
                          name {
                            firstName
                            lastName
                          }
                        }
                        owner {
                          id
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["id"] = id };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("opportunities", out var opportunitiesEl)
                || !opportunitiesEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array
                || edges.GetArrayLength() == 0)
                return null;

            var firstEdge = edges[0];
            if (!firstEdge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                return null;

            return ParseOpportunity(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch opportunity {Id} from Twenty CRM", id);
            return null;
        }
    }

    public async Task<CrmOpportunityResponse?> UpdateOpportunityAsync(string id, UpdateCrmOpportunityRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — cannot update opportunity {Id}", id);
            return null;
        }

        try
        {
            var input = new Dictionary<string, object?> { };

            if (!string.IsNullOrWhiteSpace(request.Name))
                input["name"] = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Stage))
                input["stage"] = request.Stage;

            if (request.CloseDate is not null)
                input["closeDate"] = request.CloseDate;

            if (request.Amount.HasValue || !string.IsNullOrWhiteSpace(request.CurrencyCode))
            {
                var amtInput = new Dictionary<string, object?>();
                if (request.Amount.HasValue)
                    amtInput["amountMicros"] = (long)(request.Amount.Value * 1_000_000);
                if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
                    amtInput["currencyCode"] = request.CurrencyCode;
                input["amount"] = amtInput;
            }

            input["companyId"] = request.CompanyId;
            input["pointOfContactId"] = request.PointOfContactId;
            input["ownerId"] = request.OwnerId;

            const string mutation = """
                mutation UpdateOpportunity($id: ID!, $input: OpportunityUpdateInput!) {
                  updateOpportunity(id: $id, data: $input) {
                    id
                    name
                    stage
                        closeDate
                        amount {
                          amountMicros
                          currencyCode
                        }
                    company {
                      id
                      name
                    }
                    pointOfContact {
                      id
                      name {
                        firstName
                        lastName
                      }
                    }
                    owner {
                      id
                      name {
                        firstName
                        lastName
                      }
                      userEmail
                    }
                    createdAt
                    createdBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                    updatedAt
                    updatedBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["input"] = input,
            };

            var data = await PostGraphQLAsync(mutation, variables, cancellationToken);

            if (!data.TryGetProperty("updateOpportunity", out var resultEl)
                || resultEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParseOpportunity(resultEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update opportunity {Id} in Twenty CRM", id);
            return null;
        }
    }

    public async Task<CrmOpportunityResponse?> CreateOpportunityAsync(UpdateCrmOpportunityRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — cannot create opportunity");
            return null;
        }

        try
        {
            var input = new Dictionary<string, object?> { };

            if (!string.IsNullOrWhiteSpace(request.Name))
                input["name"] = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Stage))
                input["stage"] = request.Stage;

            if (request.CloseDate is not null)
                input["closeDate"] = request.CloseDate;

            if (request.Amount.HasValue || !string.IsNullOrWhiteSpace(request.CurrencyCode))
            {
                var amtInput = new Dictionary<string, object?>();
                if (request.Amount.HasValue)
                    amtInput["amountMicros"] = (long)(request.Amount.Value * 1_000_000);
                if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
                    amtInput["currencyCode"] = request.CurrencyCode;
                input["amount"] = amtInput;
            }

            input["companyId"] = request.CompanyId;
            input["pointOfContactId"] = request.PointOfContactId;
            input["ownerId"] = request.OwnerId;

            const string mutation = """
                mutation CreateOpportunity($data: OpportunityCreateInput!) {
                  createOpportunity(data: $data) {
                    id
                    name
                    stage
                    closeDate
                    amount {
                      amountMicros
                      currencyCode
                    }
                    company {
                      id
                      name
                    }
                    pointOfContact {
                      id
                      name {
                        firstName
                        lastName
                      }
                    }
                    owner {
                      id
                      name {
                        firstName
                        lastName
                      }
                      userEmail
                    }
                    createdAt
                    createdBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                    updatedAt
                    updatedBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["data"] = input,
            };

            var data = await PostGraphQLAsync(mutation, variables, cancellationToken);

            if (!data.TryGetProperty("createOpportunity", out var resultEl)
                || resultEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParseOpportunity(resultEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create opportunity in Twenty CRM");
            return null;
        }
    }

    public async Task<IReadOnlyList<CrmTaskResponse>> GetTasksAsync(string? lookup = null, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty tasks list");
            return [];
        }

        try
        {
            var hasLookup = !string.IsNullOrWhiteSpace(lookup);

            var query = $$"""
                query Tasks($lookup: String, $first: Int) {
                  tasks(filter: {{(hasLookup ? "{ title: { ilike: $lookup } }" : "{}")}}) {
                    edges {
                      node {
                        id
                        title
                        bodyV2 {
                          markdown
                        }
                        status
                        dueAt
                        assignee {
                          id
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        taskTargets {
                          edges {
                            node {
                              id
                              targetCompany {
                                id
                                name
                              }
                              targetPerson {
                                id
                                name {
                                  firstName
                                  lastName
                                }
                              }
                              targetOpportunity {
                                id
                                name
                              }
                            }
                          }
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["lookup"] = hasLookup ? $"%{lookup}%" : null,
                ["first"] = 200,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("tasks", out var tasksEl)
                || !tasksEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("No tasks data returned from Twenty CRM GraphQL");
                return [];
            }

            var result = new List<CrmTaskResponse>();

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var parsed = ParseTask(node);
                if (parsed is not null)
                    result.Add(parsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tasks from Twenty CRM (lookup: {Lookup})", lookup);
            return [];
        }
    }

    public async Task<CrmTaskResponse?> GetTaskByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning null for task {Id}", id);
            return null;
        }

        try
        {
            const string query = """
                query GetTask($id: ID!) {
                  tasks(filter: { id: { eq: $id } }) {
                    edges {
                      node {
                        id
                        title
                        bodyV2 {
                          markdown
                        }
                        status
                        dueAt
                        assignee {
                          id
                          name {
                            firstName
                            lastName
                          }
                          userEmail
                        }
                        taskTargets {
                          edges {
                            node {
                              id
                              targetCompany {
                                id
                                name
                              }
                              targetPerson {
                                id
                                name {
                                  firstName
                                  lastName
                                }
                              }
                              targetOpportunity {
                                id
                                name
                              }
                            }
                          }
                        }
                        createdAt
                        createdBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                        updatedAt
                        updatedBy {
                          ... on WorkspaceMember {
                            name
                          }
                          ... on User {
                            name
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["id"] = id };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("tasks", out var tasksEl)
                || !tasksEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array
                || edges.GetArrayLength() == 0)
                return null;

            var firstEdge = edges[0];
            if (!firstEdge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                return null;

            return ParseTask(node);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch task {Id} from Twenty CRM", id);
            return null;
        }
    }

    public async Task<CrmTaskResponse?> UpdateTaskAsync(string id, UpdateCrmTaskRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — cannot update task {Id}", id);
            return null;
        }

        try
        {
            var input = new Dictionary<string, object?> { };

            if (!string.IsNullOrWhiteSpace(request.Title))
                input["title"] = request.Title;

            if (!string.IsNullOrWhiteSpace(request.Body))
                input["bodyV2"] = new Dictionary<string, object?>
                {
                    ["markdown"] = request.Body,
                };

            if (!string.IsNullOrWhiteSpace(request.Status))
                input["status"] = request.Status;

            if (request.DueDate is not null)
                input["dueAt"] = request.DueDate;

            if (request.AssigneeId is not null)
                input["assigneeId"] = request.AssigneeId;

            if (request.Relations is not null)
                await SyncTaskTargetsAsync(id, request.Relations, cancellationToken);

            const string mutation = """
                mutation UpdateTask($id: ID!, $input: TaskUpdateInput!) {
                  updateTask(id: $id, data: $input) {
                    id
                    title
                    bodyV2 {
                      markdown
                    }
                    status
                    dueAt
                    assignee {
                      id
                      name {
                        firstName
                        lastName
                      }
                      userEmail
                    }
                    taskTargets {
                      edges {
                        node {
                          id
                          targetCompany {
                            id
                            name
                          }
                          targetPerson {
                            id
                            name {
                              firstName
                              lastName
                            }
                          }
                          targetOpportunity {
                            id
                            name
                          }
                        }
                      }
                    }
                    createdAt
                    createdBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                    updatedAt
                    updatedBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["input"] = input,
            };

            var data = await PostGraphQLAsync(mutation, variables, cancellationToken);

            if (!data.TryGetProperty("updateTask", out var resultEl)
                || resultEl.ValueKind != JsonValueKind.Object)
                return null;

            return ParseTask(resultEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {Id} in Twenty CRM", id);
            return null;
        }
    }

    public async Task<CrmTaskResponse?> CreateTaskAsync(UpdateCrmTaskRequest request, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — cannot create task");
            return null;
        }

        try
        {
            var input = new Dictionary<string, object?> { };

            if (!string.IsNullOrWhiteSpace(request.Title))
                input["title"] = request.Title;

            if (!string.IsNullOrWhiteSpace(request.Body))
                input["bodyV2"] = new Dictionary<string, object?>
                {
                    ["markdown"] = request.Body,
                };

            if (!string.IsNullOrWhiteSpace(request.Status))
                input["status"] = request.Status;

            if (request.DueDate is not null)
                input["dueAt"] = request.DueDate;

            if (request.AssigneeId is not null)
                input["assigneeId"] = request.AssigneeId;

            const string mutation = """
                mutation CreateTask($data: TaskCreateInput!) {
                  createTask(data: $data) {
                    id
                    title
                    bodyV2 {
                      markdown
                    }
                    status
                    dueAt
                    assignee {
                      id
                      name {
                        firstName
                        lastName
                      }
                      userEmail
                    }
                    taskTargets {
                      edges {
                        node {
                          id
                          targetCompany {
                            id
                            name
                          }
                          targetPerson {
                            id
                            name {
                              firstName
                              lastName
                            }
                          }
                          targetOpportunity {
                            id
                            name
                          }
                        }
                      }
                    }
                    createdAt
                    createdBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                    updatedAt
                    updatedBy {
                      ... on WorkspaceMember {
                        name
                      }
                      ... on User {
                        name
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["data"] = input,
            };

            var data = await PostGraphQLAsync(mutation, variables, cancellationToken);

            if (!data.TryGetProperty("createTask", out var resultEl)
                || resultEl.ValueKind != JsonValueKind.Object)
                return null;

            var createdTaskId = GetStringProp(resultEl, "id");
            if (!string.IsNullOrWhiteSpace(createdTaskId) && request.Relations is not null)
                await SyncTaskTargetsAsync(createdTaskId, request.Relations, cancellationToken);

            return ParseTask(resultEl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task in Twenty CRM");
            return null;
        }
    }

    public async Task<IReadOnlyList<CrmStageOption>> GetTaskStatusOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Introspect the Task type to discover the status field's type info
            var enumTypeName = await DiscoverTaskStatusEnumTypeNameAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(enumTypeName))
            {
                // Step 2: Query enum values from the discovered type
                var result = await QueryEnumValuesAsync(enumTypeName, cancellationToken);
                if (result.Count > 0)
                    return result;
            }

            // Step 3: Try common type name patterns as fallback
            var candidates = new[] { "TaskStatus", "TaskStatusType", "TaskStatusEnum", "StatusType" };
            foreach (var candidate in candidates)
            {
                var result = await QueryEnumValuesAsync(candidate, cancellationToken);
                if (result.Count > 0)
                    return result;
            }

            _logger.LogWarning("All introspection attempts failed for Task status options");
            return Array.Empty<CrmStageOption>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Task status options");
            return Array.Empty<CrmStageOption>();
        }
    }

    public async Task<IReadOnlyList<CrmTimelineItemResponse>> GetCompanyTimelineAsync(string companyId, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty timeline");
            return [];
        }

        try
        {
            const string query = """
                query CompanyTimeline($companyId: String!) {
                  companies(filter: { id: { eq: $companyId } }) {
                    edges {
                      node {
                        timelineActivities {
                          edges {
                            node {
                              id
                              name
                              properties
                              createdAt
                              workspaceMemberId
                              createdBy {
                                source
                                workspaceMemberId
                                name
                                context
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["companyId"] = companyId,
            };

            var data = await PostGraphQLAsync(query, variables, cancellationToken);

            if (!data.TryGetProperty("companies", out var companiesEl)
                || !companiesEl.TryGetProperty("edges", out var companyEdges)
                || companyEdges.ValueKind != JsonValueKind.Array
                || companyEdges.GetArrayLength() == 0)
            {
                return [];
            }

            var companyNode = companyEdges[0];
            if (!companyNode.TryGetProperty("node", out var company) || company.ValueKind != JsonValueKind.Object)
                return [];

            if (!company.TryGetProperty("timelineActivities", out var timelineEl)
                || !timelineEl.TryGetProperty("edges", out var edges)
                || edges.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<CrmTimelineItemResponse>();

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var id = GetStringProp(node, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var name = GetStringProp(node, "name");
                var type = string.IsNullOrWhiteSpace(name) ? "event" : name;
                var actorName = ResolveTimelineActorName(node);
                var body = FormatTimelineBody(name, node);

                result.Add(new CrmTimelineItemResponse
                {
                    Id = id,
                    Type = type,
                    Title = HumanizeTimelineName(name),
                    Body = body,
                    CreatedOn = GetStringProp(node, "createdAt"),
                    CreatedBy = actorName,
                });
            }

            return result.OrderByDescending(i => i.CreatedOn).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch timeline for company {CompanyId} from Twenty CRM", companyId);
            return [];
        }
    }

    private static string ResolveTimelineActorName(JsonElement node)
    {
        if (node.TryGetProperty("createdBy", out var createdBy) && createdBy.ValueKind == JsonValueKind.Object)
        {
            var name = GetStringProp(createdBy, "name");
            if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name, "System", StringComparison.OrdinalIgnoreCase))
                return name;
        }

        return string.Empty;
    }

    private static string FormatTimelineBody(string? name, JsonElement node)
    {
        if (!node.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return string.Empty;

        if (!props.TryGetProperty("diff", out var diff) || diff.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var parts = new List<string>();

        foreach (var field in diff.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.Object)
                continue;

            var after = field.Value.TryGetProperty("after", out var afterVal) ? afterVal : default;
            var before = field.Value.TryGetProperty("before", out var beforeVal) ? beforeVal : default;

            var afterStr = FormatTimelineValue(after);
            var beforeStr = FormatTimelineValue(before);

            if (string.IsNullOrWhiteSpace(afterStr) && string.IsNullOrWhiteSpace(beforeStr))
                continue;

            var fieldName = HumanizeFieldName(field.Name);

            if (string.IsNullOrWhiteSpace(beforeStr))
                parts.Add($"{fieldName}: {afterStr}");
            else if (string.IsNullOrWhiteSpace(afterStr))
                parts.Add($"{fieldName}: (removed) was {beforeStr}");
            else
                parts.Add($"{fieldName}: {afterStr} (was {beforeStr})");
        }

        return string.Join("\n", parts);
    }

    private static string FormatTimelineValue(JsonElement val)
    {
        if (val.ValueKind == JsonValueKind.Null || val.ValueKind == JsonValueKind.Undefined)
            return string.Empty;

        if (val.ValueKind == JsonValueKind.String)
            return val.GetString() ?? string.Empty;

        if (val.ValueKind == JsonValueKind.True || val.ValueKind == JsonValueKind.False)
            return val.GetBoolean() ? "Yes" : "No";

        if (val.ValueKind == JsonValueKind.Number)
            return val.GetRawText();

        if (val.ValueKind == JsonValueKind.Object)
        {
            if (val.TryGetProperty("name", out var nameVal) && nameVal.ValueKind == JsonValueKind.String)
                return nameVal.GetString() ?? string.Empty;
            if (val.TryGetProperty("displayName", out var dn) && dn.ValueKind == JsonValueKind.String)
                return dn.GetString() ?? string.Empty;
            if (val.TryGetProperty("primaryLinkUrl", out var link) && link.ValueKind == JsonValueKind.String)
                return link.GetString() ?? string.Empty;
            return val.GetRawText();
        }

        return val.GetRawText();
    }

    private static string HumanizeTimelineName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Event";

        var parts = name.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        }
        return string.Join(" ", parts);
    }

    private static string HumanizeFieldName(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return field;

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < field.Length; i++)
        {
            if (i == 0)
                sb.Append(char.ToUpperInvariant(field[i]));
            else if (char.IsUpper(field[i]))
                sb.Append(' ').Append(field[i]);
            else
                sb.Append(field[i]);
        }
        return sb.ToString().Replace("Id", "ID").Replace("Url", "URL");
    }

    private async Task<string?> DiscoverTaskStatusEnumTypeNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            const string discoverQuery = """
                {
                  __type(name: "Task") {
                    fields {
                      name
                      type {
                        name
                        kind
                        ofType {
                          name
                          kind
                        }
                      }
                    }
                  }
                }
                """;

            var data = await PostGraphQLAsync(discoverQuery, new Dictionary<string, object?>(), cancellationToken);

            if (!data.TryGetProperty("__type", out var typeEl)
                || !typeEl.TryGetProperty("fields", out var fields)
                || fields.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("__type(name: 'Task') introspection returned no fields");
                return null;
            }

            foreach (var f in fields.EnumerateArray())
            {
                if (GetStringProp(f, "name") != "status")
                    continue;

                if (!f.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.Object)
                    break;

                var kind = GetStringProp(t, "kind");
                var name = GetStringProp(t, "name");

                _logger.LogWarning("Task.status field type: kind={Kind}, name={Name}", kind, name);

                // Direct enum type (nullable)
                if (kind == "ENUM" && !string.IsNullOrWhiteSpace(name))
                    return name;

                // Wrapped enum (non-null: NON_NULL -> ENUM)
                if (t.TryGetProperty("ofType", out var ofType) && ofType.ValueKind == JsonValueKind.Object)
                {
                    var ofKind = GetStringProp(ofType, "kind");
                    var ofName = GetStringProp(ofType, "name");
                    _logger.LogWarning("Task.status field ofType: kind={Kind}, name={Name}", ofKind, ofName);

                    if (ofKind == "ENUM" && !string.IsNullOrWhiteSpace(ofName))
                        return ofName;
                }

                break;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Task status enum type name");
            return null;
        }
    }

    private async Task<IReadOnlyList<CrmStageOption>> QueryEnumValuesAsync(string typeName, CancellationToken cancellationToken)
    {
        try
        {
            var query = string.Concat(
                "{ __type(name: \"", typeName, "\") { enumValues { name description } } }");

            var data = await PostGraphQLAsync(query, new Dictionary<string, object?>(), cancellationToken);

            if (data.TryGetProperty("__type", out var typeEl)
                && typeEl.ValueKind == JsonValueKind.Object)
            {
                if (typeEl.TryGetProperty("enumValues", out var vals)
                    && vals.ValueKind == JsonValueKind.Array)
                {
                    var list = vals.EnumerateArray()
                        .Select(v => new CrmStageOption
                        {
                            Value = GetStringProp(v, "name"),
                            Label = GetStringProp(v, "description") ?? HumanizeEnumName(GetStringProp(v, "name")),
                        })
                        .Where(o => !string.IsNullOrWhiteSpace(o.Value))
                        .ToList();

                    if (list.Count > 0)
                    {
                        list.Insert(0, new CrmStageOption { Value = "", Label = "No Status" });
                    }

                    return list;
                }

                // __type found but has no enumValues — log the kind
                var kind = GetStringProp(typeEl, "kind");
                _logger.LogWarning("__type(name: '{Type}') found with kind={Kind} but no enumValues", typeName, kind);
            }

            return Array.Empty<CrmStageOption>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QueryEnumValuesAsync failed for type '{Type}'", typeName);
            return Array.Empty<CrmStageOption>();
        }
    }

    private static string ResolveTaskBody(JsonElement node)
    {
        if (!node.TryGetProperty("bodyV2", out var bodyEl) || bodyEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetStringProp(bodyEl, "markdown");
    }

    private static CrmTaskResponse? ParseTask(JsonElement node)
    {
        var taskId = GetStringProp(node, "id");
        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        var assignee = string.Empty;
        var assigneeId = string.Empty;
        if (node.TryGetProperty("assignee", out var assigneeEl) && assigneeEl.ValueKind == JsonValueKind.Object)
        {
            assigneeId = GetStringProp(assigneeEl, "id");
            var name = GetCompositeName(assigneeEl, "name");
            var email = GetStringProp(assigneeEl, "userEmail");
            assignee = !string.IsNullOrWhiteSpace(name) ? name : email;
        }

        return new CrmTaskResponse
        {
            Id = taskId,
            Title = GetStringProp(node, "title"),
            Body = ResolveTaskBody(node),
            Status = GetStringProp(node, "status"),
            DueDate = GetStringProp(node, "dueAt"),
            Assignee = assignee,
            AssigneeId = assigneeId,
            Relations = ResolveTaskTargets(node),
            CreatedOn = GetStringProp(node, "createdAt"),
            CreatedBy = ResolveActorName(node, "createdBy"),
            UpdatedOn = GetStringProp(node, "updatedAt"),
            UpdatedBy = ResolveActorName(node, "updatedBy"),
        };
    }

    private static List<CrmTaskRelationResponse> ResolveTaskTargets(JsonElement node)
    {
        var items = new List<CrmTaskRelationResponse>();

        if (!node.TryGetProperty("taskTargets", out var targetsEl)
            || !targetsEl.TryGetProperty("edges", out var edges)
            || edges.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var targetNode) || targetNode.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetStringProp(targetNode, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (targetNode.TryGetProperty("targetCompany", out var company)
                && company.ValueKind == JsonValueKind.Object)
            {
                var name = GetStringProp(company, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(new CrmTaskRelationResponse
                    {
                        Id = GetStringProp(company, "id"),
                        Name = name,
                        Type = "Company",
                    });
                }
            }

            if (targetNode.TryGetProperty("targetPerson", out var person)
                && person.ValueKind == JsonValueKind.Object)
            {
                var name = GetCompositeName(person, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(new CrmTaskRelationResponse
                    {
                        Id = GetStringProp(person, "id"),
                        Name = name,
                        Type = "Person",
                    });
                }
            }

            if (targetNode.TryGetProperty("targetOpportunity", out var opp)
                && opp.ValueKind == JsonValueKind.Object)
            {
                var name = GetStringProp(opp, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    items.Add(new CrmTaskRelationResponse
                    {
                        Id = GetStringProp(opp, "id"),
                        Name = name,
                        Type = "Opportunity",
                    });
                }
            }
        }

        return items;
    }

    private static List<string> ExtractRelationIds(JsonElement node, string relationField)
    {
        var ids = new List<string>();

        if (!node.TryGetProperty(relationField, out var relation) || relation.ValueKind != JsonValueKind.Object)
            return ids;

        if (!relation.TryGetProperty("edges", out var edges) || edges.ValueKind != JsonValueKind.Array)
            return ids;

        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var child) || child.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetStringProp(child, "id");
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add(id);
        }

        return ids;
    }

    private async Task SyncCompanyRelationAsync(
        string companyId,
        string objectType,
        IReadOnlyList<string> currentIds,
        IReadOnlyList<string> desiredIds,
        CancellationToken cancellationToken)
    {
        var desiredSet = new HashSet<string>(desiredIds, StringComparer.OrdinalIgnoreCase);
        var currentSet = new HashSet<string>(currentIds, StringComparer.OrdinalIgnoreCase);

        var toConnect = desiredIds.Where(id => !currentSet.Contains(id)).ToList();
        var toDisconnect = currentIds.Where(id => !desiredSet.Contains(id)).ToList();

        foreach (var id in toConnect)
        {
            await SetObjectCompanyAsync(objectType, id, companyId, cancellationToken);
        }

        foreach (var id in toDisconnect)
        {
            await SetObjectCompanyAsync(objectType, id, null, cancellationToken);
        }
    }

    private async Task SetObjectCompanyAsync(string objectType, string objectId, string? companyId, CancellationToken cancellationToken)
    {
        try
        {
            var mutationName = objectType == "person" ? "updatePerson" : "updateOpportunity";
            var inputType = objectType == "person" ? "PersonUpdateInput" : "OpportunityUpdateInput";

            var query = $$"""
                mutation SetCompany($id: ID!, $data: {{inputType}}!) {
                  {{mutationName}}(id: $id, data: $data) {
                    id
                  }
                }
                """;

            var updateData = new Dictionary<string, object?> { ["companyId"] = companyId };

            var variables = new Dictionary<string, object?>
            {
                ["id"] = objectId,
                ["data"] = updateData,
            };

            await PostGraphQLAsync(query, variables, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set company {CompanyId} on {Type} {ObjectId}", companyId, objectType, objectId);
            throw;
        }
    }

    private static IReadOnlyList<CrmCatalogItem> ExtractCatalogItems(JsonElement data, string field, Func<JsonElement, string> nameSelector)
    {
        if (!data.TryGetProperty(field, out var itemsEl)
            || !itemsEl.TryGetProperty("edges", out var edges)
            || edges.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<CrmCatalogItem>();

        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetStringProp(node, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var name = nameSelector(node);
            if (string.IsNullOrWhiteSpace(name))
                name = id;

            result.Add(new CrmCatalogItem { Id = id, Name = name });
        }

        return result;
    }

    private static string NormalizeDomainUrl(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return string.Empty;

        domain = domain.Trim();

        if (domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return domain;

        return $"https://{domain}";
    }

    private CrmCompanyResponse? ParseCompany(JsonElement node)
    {
        var companyId = GetStringProp(node, "id");
        if (string.IsNullOrWhiteSpace(companyId))
            return null;

        return new CrmCompanyResponse
        {
            Id = companyId,
            Name = GetStringProp(node, "name"),
            AccountOwner = ResolveAccountOwnerName(node),
            AccountOwnerId = GetStringProp(node, "accountOwnerId"),
            DomainName = ResolveDomainName(node),
            Address = ParseAddress(node),
            FormattedAddress = FormatAddress(ParseAddress(node)),
            CreatedOn = GetStringProp(node, "createdAt"),
            CreatedBy = ResolveActorName(node, "createdBy"),
            UpdatedOn = GetStringProp(node, "updatedAt"),
            UpdatedBy = ResolveActorName(node, "updatedBy"),
            People = ResolveRelationItems(node, "people"),
            Opportunities = ResolveRelationItems(node, "opportunities"),
        };
    }

    private async Task<JsonElement> PostGraphQLAsync(
        string query,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

        var baseUrl = options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/graphql";

        var payload = new JsonObject
        {
            ["query"] = query,
            ["variables"] = JsonSerializer.SerializeToNode(variables),
        };

        using var httpContent = new StringContent(
            payload.ToJsonString(),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(url, httpContent, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Twenty CRM GraphQL returned {StatusCode}. Body: {Body}",
                (int)response.StatusCode,
                Truncate(body));
            throw new InvalidOperationException($"Twenty CRM GraphQL error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            var firstError = errors.EnumerateArray().FirstOrDefault();
            var errorMessage = firstError.ValueKind == JsonValueKind.Object
                ? GetStringProp(firstError, "message")
                : errors.GetRawText();

            if (string.IsNullOrWhiteSpace(errorMessage))
                errorMessage = "Unknown GraphQL error";

            _logger.LogWarning("Twenty CRM GraphQL returned errors: {Errors}", Truncate(errors.GetRawText()));
            throw new InvalidOperationException($"Twenty CRM GraphQL error: {errorMessage}");
        }

        if (!doc.RootElement.TryGetProperty("data", out var data))
            return default;

        return data.Clone();
    }

    private static string ResolveAccountOwnerName(JsonElement company)
    {
        if (!company.TryGetProperty("accountOwner", out var owner) || owner.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var name = GetCompositeName(owner, "name");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return GetStringProp(owner, "userEmail");
    }

    private static string ResolveActorName(JsonElement parent, string field)
    {
        if (!parent.TryGetProperty(field, out var actor) || actor.ValueKind != JsonValueKind.Object)
            return string.Empty;

        return GetStringProp(actor, "name");
    }

    private static List<string> ResolveRelationNames(
        JsonElement company,
        string relationField,
        Func<JsonElement, string> selector)
    {
        var names = new List<string>();

        if (!company.TryGetProperty(relationField, out var relation) || relation.ValueKind != JsonValueKind.Object)
            return names;

        if (!relation.TryGetProperty("edges", out var edges) || edges.ValueKind != JsonValueKind.Array)
            return names;

        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                continue;

            var name = selector(node);
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name);
        }

        return names;
    }

    private static List<CrmRelationItem> ResolveRelationItems(JsonElement company, string relationField)
    {
        var items = new List<CrmRelationItem>();

        if (!company.TryGetProperty(relationField, out var relation) || relation.ValueKind != JsonValueKind.Object)
            return items;

        if (!relation.TryGetProperty("edges", out var edges) || edges.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetStringProp(node, "id");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string name;
            if (relationField == "people")
                name = GetCompositeName(node, "name");
            else
                name = GetStringProp(node, "name");

            if (string.IsNullOrWhiteSpace(name))
                name = id;

            items.Add(new CrmRelationItem { Id = id, Name = name });
        }

        return items;
    }

    private static string GetCompositeName(JsonElement parent, string field)
    {
        if (!parent.TryGetProperty(field, out var nameEl) || nameEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var firstName = GetStringProp(nameEl, "firstName");
        var lastName = GetStringProp(nameEl, "lastName");
        return string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string GetStringProp(JsonElement obj, string key)
    {
        return obj.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string ResolveDomainName(JsonElement company)
    {
        if (!company.TryGetProperty("domainName", out var domainEl) || domainEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var url = GetStringProp(domainEl, "primaryLinkUrl");
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url["https://".Length..];

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return url["http://".Length..];

        return url;
    }

    private static CrmAddress ParseAddress(JsonElement company)
    {
        var address = new CrmAddress();

        if (!company.TryGetProperty("address", out var addressEl) || addressEl.ValueKind != JsonValueKind.Object)
            return address;

        address.Street1 = GetStringProp(addressEl, "addressStreet1").Trim();
        address.Street2 = GetStringProp(addressEl, "addressStreet2").Trim();
        address.City = GetStringProp(addressEl, "addressCity").Trim();
        address.State = GetStringProp(addressEl, "addressState").Trim();
        address.Postcode = GetStringProp(addressEl, "addressPostcode").Trim();
        address.Country = GetStringProp(addressEl, "addressCountry").Trim();

        return address;
    }

    private static string FormatAddress(CrmAddress address)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(address.Street1))
            parts.Add(address.Street1);
        if (!string.IsNullOrWhiteSpace(address.Street2))
            parts.Add(address.Street2);

        var cityLine = string.Join(" ", new[] { address.City, address.State, address.Postcode }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(cityLine))
            parts.Add(cityLine);

        if (!string.IsNullOrWhiteSpace(address.Country))
            parts.Add(address.Country);

        return string.Join(", ", parts);
    }

    private static string Truncate(string value, int max = 2000)
    {
        return value.Length <= max ? value : value[..max] + "...<truncated>";
    }

    private async Task SyncTaskTargetsAsync(
        string taskId,
        IReadOnlyList<CrmTaskRelationRequest>? desiredRelations,
        CancellationToken cancellationToken)
    {
        if (desiredRelations is null)
            return;

        // Fetch current task targets
        var currentTargets = new List<(string targetId, string entityId, string entityType)>();
        var query = """
            query GetCurrentTaskTargets($id: ID!) {
              tasks(filter: { id: { eq: $id } }) {
                edges {
                  node {
                    id
                    taskTargets {
                      edges {
                        node {
                          id
                          targetCompany { id }
                          targetPerson { id }
                          targetOpportunity { id }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var vars = new Dictionary<string, object?> { ["id"] = taskId };
        var data = await PostGraphQLAsync(query, vars, cancellationToken);

        if (data.TryGetProperty("tasks", out var tasksEl)
            && tasksEl.TryGetProperty("edges", out var curEdges)
            && curEdges.ValueKind == JsonValueKind.Array
            && curEdges.GetArrayLength() > 0
            && curEdges[0].TryGetProperty("node", out var curNode)
            && curNode.TryGetProperty("taskTargets", out var targetsEl)
            && targetsEl.TryGetProperty("edges", out var targetEdges)
            && targetEdges.ValueKind == JsonValueKind.Array)
        {
            foreach (var edge in targetEdges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var targetId = GetStringProp(node, "id");
                if (string.IsNullOrWhiteSpace(targetId))
                    continue;

                if (node.TryGetProperty("targetCompany", out var c) && c.ValueKind == JsonValueKind.Object)
                    currentTargets.Add((targetId, GetStringProp(c, "id"), "Company"));
                if (node.TryGetProperty("targetPerson", out var p) && p.ValueKind == JsonValueKind.Object)
                    currentTargets.Add((targetId, GetStringProp(p, "id"), "Person"));
                if (node.TryGetProperty("targetOpportunity", out var o) && o.ValueKind == JsonValueKind.Object)
                    currentTargets.Add((targetId, GetStringProp(o, "id"), "Opportunity"));
            }
        }

        var desiredSet = new HashSet<string>(desiredRelations.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
        var currentSet = new HashSet<string>(currentTargets.Select(t => t.entityId), StringComparer.OrdinalIgnoreCase);

        var toCreate = desiredRelations.Where(r => !currentSet.Contains(r.Id)).ToList();
        var toDelete = currentTargets.Where(t => !desiredSet.Contains(t.entityId)).ToList();

        foreach (var target in toDelete)
        {
            await DeleteTaskTargetAsync(target.targetId, cancellationToken);
        }

        foreach (var rel in toCreate)
        {
            await CreateTaskTargetAsync(taskId, rel.Id, rel.Type, cancellationToken);
        }
    }

    private async Task CreateTaskTargetAsync(string taskId, string entityId, string entityType, CancellationToken cancellationToken)
    {
        var field = entityType switch
        {
            "Company" => "targetCompanyId",
            "Person" => "targetPersonId",
            "Opportunity" => "targetOpportunityId",
            _ => throw new InvalidOperationException($"Unknown relation type: {entityType}"),
        };

        var mutation = """
            mutation CreateTaskTarget($data: TaskTargetCreateInput!) {
              createTaskTarget(data: $data) {
                id
              }
            }
            """;

        var input = new Dictionary<string, object?>
        {
            ["taskId"] = taskId,
            [field] = entityId,
        };

        var variables = new Dictionary<string, object?>
        {
            ["data"] = input,
        };

        await PostGraphQLAsync(mutation, variables, cancellationToken);
    }

    private async Task DeleteTaskTargetAsync(string targetId, CancellationToken cancellationToken)
    {
        var mutation = """
            mutation DeleteTaskTarget($id: ID!) {
              deleteTaskTarget(id: $id) {
                id
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["id"] = targetId,
        };

        await PostGraphQLAsync(mutation, variables, cancellationToken);
    }
}
