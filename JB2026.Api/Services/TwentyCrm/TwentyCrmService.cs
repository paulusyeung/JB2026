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

                var companyId = GetStringProp(node, "id");
                if (string.IsNullOrWhiteSpace(companyId))
                    continue;

                result.Add(new CrmCompanyResponse
                {
                    Id = companyId,
                    Name = GetStringProp(node, "name"),
                    AccountOwner = ResolveAccountOwnerName(node),
                    DomainName = ResolveDomainName(node),
                    Address = FormatAddress(node),
                    CreatedOn = GetStringProp(node, "createdAt"),
                    CreatedBy = ResolveActorName(node, "createdBy"),
                    UpdatedOn = GetStringProp(node, "updatedAt"),
                    UpdatedBy = ResolveActorName(node, "updatedBy"),
                    People = ResolveRelationNames(node, "people", FormatPersonName),
                    Opportunities = ResolveRelationNames(node, "opportunities", n => GetStringProp(n, "name")),
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch companies from Twenty CRM");
            return [];
        }
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
            _logger.LogWarning("Twenty CRM GraphQL returned errors: {Errors}", Truncate(errors.GetRawText()));
            throw new InvalidOperationException("Twenty CRM GraphQL returned errors");
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

    private static string ResolveRelationNames(
        JsonElement company,
        string relationField,
        Func<JsonElement, string> selector)
    {
        if (!company.TryGetProperty(relationField, out var relation) || relation.ValueKind != JsonValueKind.Object)
            return string.Empty;

        if (!relation.TryGetProperty("edges", out var edges) || edges.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var names = new List<string>();
        foreach (var edge in edges.EnumerateArray())
        {
            if (!edge.TryGetProperty("node", out var node) || node.ValueKind != JsonValueKind.Object)
                continue;

            var name = selector(node);
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name);
        }

        if (names.Count == 0)
            return string.Empty;

        if (names.Count == 1)
            return names[0];

        return $"{names[0]}...";
    }

    private static string FormatPersonName(JsonElement person)
    {
        var firstName = GetStringProp(person, "firstName");
        var lastName = GetStringProp(person, "lastName");
        return string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
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

    private static string FormatAddress(JsonElement company)
    {
        if (!company.TryGetProperty("address", out var addressEl))
            return string.Empty;

        if (addressEl.ValueKind != JsonValueKind.Object)
            return string.Empty;

        var parts = new List<string>();
        foreach (var field in new[] { "addressStreet1", "addressStreet2", "addressCity", "addressState", "addressPostcode", "addressCountry" })
        {
            var s = GetStringProp(addressEl, field).Trim();
            if (!string.IsNullOrWhiteSpace(s))
                parts.Add(s);
        }

        return string.Join(", ", parts);
    }

    private static string Truncate(string value, int max = 2000)
    {
        return value.Length <= max ? value : value[..max] + "...<truncated>";
    }
}
