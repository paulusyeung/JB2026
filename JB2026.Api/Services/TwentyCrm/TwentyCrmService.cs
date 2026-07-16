using System.Text.Json;
using System.Text.Json.Nodes;
using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services.TwentyCrm;

public class TwentyCrmService : ITwentyCrmService
{
    private readonly IOptions<TwentyCrmOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwentyCrmService> _logger;

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
                          name
                        }
                        updatedAt
                        updatedBy {
                          name
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

            foreach (var edge in edges.EnumerateArray())
            {
                if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                    continue;

                var parsed = ParseCompany(node);
                if (parsed is not null)
                    result.Add(parsed);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch companies from Twenty CRM");
            return [];
        }
    }

    public async Task<CrmCompanyResponse?> GetCompanyByIdAsync(string id, CancellationToken cancellationToken = default)
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
                          name
                        }
                        updatedAt
                        updatedBy {
                          name
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

            return ParseCompany(companyEl);
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

    public async Task<IReadOnlyList<CrmCatalogItem>> GetPeopleAsync(string? lookup = null, CancellationToken cancellationToken = default)
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

            return ExtractCatalogItems(data, "people", node => GetCompositeName(node, "name"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch people from Twenty CRM");
            return [];
        }
    }

    public async Task<IReadOnlyList<CrmCatalogItem>> GetOpportunitiesAsync(string? lookup = null, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogWarning("Twenty CRM not configured — returning empty opportunities list");
            return [];
        }

        try
        {
            var hasLookup = !string.IsNullOrWhiteSpace(lookup);

            var query = $$"""
                query Opportunities($lookup: String, $first: Int) {
                  opportunities(filter: {{(hasLookup ? "{ name: { ilike: $lookup } }" : "{}")}}) {
                    edges {
                      node {
                        id
                        name
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

            return ExtractCatalogItems(data, "opportunities", node => GetStringProp(node, "name"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch opportunities from Twenty CRM");
            return [];
        }
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
}
