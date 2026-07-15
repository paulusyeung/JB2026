using System.Text.Json;
using JB2026.Api.Models;
using JB2026.Api.Options;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services.TwentyCrm;

public class TwentyCrmService : ITwentyCrmService
{
    private readonly IOptions<TwentyCrmOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwentyCrmService> _logger;

    private static readonly JsonSerializerOptions JsonUnescapedOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

        var baseUrl = options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/rest/workspaceMembers?limit=100";

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twenty CRM REST returned {StatusCode} for {Email}. Body: {Body}",
                    (int)response.StatusCode, email, Truncate(body));
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var members = doc.RootElement
                .GetProperty("data")
                .GetProperty("workspaceMembers");

            foreach (var member in members.EnumerateArray())
            {
                if (member.TryGetProperty("userEmail", out var userEmailEl)
                    && userEmailEl.ValueKind == JsonValueKind.String
                    && string.Equals(userEmailEl.GetString(), email, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);

        var baseUrl = options.BaseUrl.TrimEnd('/');

        var workspaceMembers = await FetchAllWorkspaceMembersAsync(client, baseUrl, cancellationToken);
        var companies = await FetchAllCompaniesAsync(client, baseUrl, cancellationToken);

        var emailToMember = workspaceMembers
            .Where(m => m.TryGetValue("userEmail", out var e) && e.ValueKind == JsonValueKind.String)
            .ToDictionary(
                m => m["userEmail"].GetString()!,
                m => m,
                StringComparer.OrdinalIgnoreCase);

        var idToMember = workspaceMembers
            .Where(m => m.TryGetValue("id", out var e) && e.ValueKind == JsonValueKind.String)
            .ToDictionary(
                m => m["id"].GetString()!,
                m => m,
                StringComparer.OrdinalIgnoreCase);

        var result = new List<CrmCompanyResponse>(companies.Count);

        foreach (var company in companies)
        {
            var companyId = GetStringProp(company, "id");
            if (string.IsNullOrWhiteSpace(companyId))
                continue;

            var accountOwnerId = GetStringProp(company, "accountOwnerId");

            var (accountOwnerEmail, ownerName) = ResolveAccountOwner(accountOwnerId, idToMember, emailToMember);

            if (!ShouldIncludeCompany(accountOwnerEmail, currentUserEmail))
                continue;

            if (!string.IsNullOrWhiteSpace(lookup))
            {
                var name = GetStringProp(company, "name");
                var domain = GetStringProp(company, "domainName");
                if (!ContainsIgnoreCase(name, lookup) && !ContainsIgnoreCase(domain, lookup))
                    continue;
            }

            result.Add(new CrmCompanyResponse
            {
                Id = companyId,
                Name = GetStringProp(company, "name"),
                AccountOwner = ownerName,
                DomainName = GetStringProp(company, "domainName"),
                Address = FormatAddress(company),
                CreatedOn = FormatTimestamp(GetStringProp(company, "createdAt")),
                CreatedBy = ResolveMemberName(GetStringProp(company, "createdBy"), idToMember),
                UpdatedOn = FormatTimestamp(GetStringProp(company, "updatedAt")),
                UpdatedBy = ResolveMemberName(GetStringProp(company, "updatedBy"), idToMember),
                PeopleCount = CountArray(company, "people"),
                OpportunitiesCount = CountArray(company, "opportunities"),
            });
        }

        return result;
    }

    private async Task<List<Dictionary<string, JsonElement>>> FetchAllWorkspaceMembersAsync(
        HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        var members = new List<Dictionary<string, JsonElement>>();

        try
        {
            var url = $"{baseUrl}/rest/workspaceMembers?limit=100";
            var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twenty CRM returned {StatusCode} for workspaceMembers", (int)response.StatusCode);
                return members;
            }

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            var rawMembers = ResolveProperty(data, "workspaceMembers");

            members.AddRange(ParseObjectArray(rawMembers));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch workspace members from Twenty CRM");
        }

        return members;
    }

    private async Task<List<Dictionary<string, JsonElement>>> FetchAllCompaniesAsync(
        HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        var allCompanies = new List<Dictionary<string, JsonElement>>();

        try
        {
            var url = $"{baseUrl}/rest/companies?limit=100";
            var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twenty CRM returned {StatusCode} for companies", (int)response.StatusCode);
                return allCompanies;
            }

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            var rawCompanies = ResolveProperty(data, "companies");

            allCompanies.AddRange(ParseObjectArray(rawCompanies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch companies from Twenty CRM");
        }

        return allCompanies;
    }

    private static JsonElement ResolveProperty(JsonElement data, string propertyName)
    {
        if (!data.TryGetProperty(propertyName, out var prop))
            return default;

        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("edges", out var edges))
        {
            return edges;
        }

        return prop;
    }

    private static List<Dictionary<string, JsonElement>> ParseObjectArray(JsonElement element)
    {
        var result = new List<Dictionary<string, JsonElement>>();

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    if (item.TryGetProperty("node", out var node))
                    {
                        result.Add(FlattenObject(node));
                    }
                    else
                    {
                        result.Add(FlattenObject(item));
                    }
                }
            }
        }

        return result;
    }

    private static Dictionary<string, JsonElement> FlattenObject(JsonElement obj)
    {
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.Clone();
        }
        return dict;
    }

    private static string GetStringProp(Dictionary<string, JsonElement> obj, string key)
    {
        return obj.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string FormatAddress(Dictionary<string, JsonElement> company)
    {
        if (!company.TryGetValue("address", out var addressEl) || addressEl.ValueKind != JsonValueKind.Object)
        {
            if (addressEl.ValueKind == JsonValueKind.String)
                return addressEl.GetString() ?? string.Empty;
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var field in new[] { "addressStreet1", "addressStreet2", "addressCity", "addressState", "addressPostcode", "addressCountry" })
        {
            if (addressEl.TryGetProperty(field, out var val) && val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    parts.Add(s);
            }
        }

        return string.Join(", ", parts);
    }

    private static string FormatTimestamp(string timestamp)
    {
        return timestamp;
    }

    private static (string Email, string Name) ResolveAccountOwner(
        string? accountOwnerId,
        Dictionary<string, Dictionary<string, JsonElement>> idToMember,
        Dictionary<string, Dictionary<string, JsonElement>> emailToMember)
    {
        if (string.IsNullOrWhiteSpace(accountOwnerId))
            return (string.Empty, string.Empty);

        if (!idToMember.TryGetValue(accountOwnerId, out var member))
            return (string.Empty, string.Empty);

        var email = GetStringProp(member, "userEmail");
        var name = GetDisplayName(member, "name");
        if (string.IsNullOrWhiteSpace(name))
            name = email;
        return (email, name);
    }

    private static string ResolveMemberName(
        string? memberId,
        Dictionary<string, Dictionary<string, JsonElement>> idToMember)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return string.Empty;

        return idToMember.TryGetValue(memberId, out var member)
            ? GetDisplayName(member, "name")
            : string.Empty;
    }

    private static string GetDisplayName(Dictionary<string, JsonElement> obj, string key)
    {
        if (!obj.TryGetValue(key, out var el))
            return string.Empty;

        if (el.ValueKind == JsonValueKind.String)
            return el.GetString() ?? string.Empty;

        if (el.ValueKind == JsonValueKind.Object)
        {
            var firstName = GetJsonPropertyString(el, "firstName");
            var lastName = GetJsonPropertyString(el, "lastName");
            return string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return string.Empty;
    }

    private static string GetJsonPropertyString(JsonElement obj, string propertyName)
    {
        return obj.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool ShouldIncludeCompany(string accountOwnerEmail, string? currentUserEmail)
    {
        if (string.IsNullOrWhiteSpace(accountOwnerEmail))
            return true;

        if (!string.IsNullOrWhiteSpace(currentUserEmail))
            return string.Equals(accountOwnerEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static int CountArray(Dictionary<string, JsonElement> obj, string key)
    {
        if (!obj.TryGetValue(key, out var el))
            return 0;

        if (el.ValueKind == JsonValueKind.Array)
            return el.GetArrayLength();

        return 0;
    }

    private static bool ContainsIgnoreCase(string? value, string? search)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(search))
            return false;
        return value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int max = 2000)
    {
        return value.Length <= max ? value : value[..max] + "...<truncated>";
    }
}
