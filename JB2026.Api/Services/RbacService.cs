using System.Xml.Linq;

namespace JB2026.Api.Services;

public sealed class RbacService : IRbacService
{
    private const string MetadataRoot = "Metadata";
    private const string RbacElement = "Rbac";
    private const string RoleElement = "Role";
    private const string RoleAttribute = "name";
    private const string EntryElement = "entry";
    private const string KeyAttribute = "key";
    private const string ValueAttribute = "value";
    private const string OperatorRoleName = "operator";

    private readonly ISystemInfoStoredProcedureGateway _systemInfoGateway;
    private readonly IUserInfoStoredProcedureGateway _userInfoGateway;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    public RbacService(
        ISystemInfoStoredProcedureGateway systemInfoGateway,
        IUserInfoStoredProcedureGateway userInfoGateway,
        ICurrentUserProfileService currentUserProfileService)
    {
        _systemInfoGateway = systemInfoGateway;
        _userInfoGateway = userInfoGateway;
        _currentUserProfileService = currentUserProfileService;
    }

    public async Task<RbacSnapshot> GetGroupRbacAsync(string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required to read group RBAC.", nameof(role));
        }

        var snapshot = await _systemInfoGateway.SelectFirstAsync(cancellationToken);
        return new RbacSnapshot(snapshot?.OwnerName, ParseRbacValues(snapshot?.MetadataXml, role));
    }

    // Resolves the effective RBAC for the currently authenticated user using the
    // precedence: User RBAC (UserInfo.MetadataXml) -> Group RBAC by role
    // (SystemInfo.MetadataXml <Role>) -> all visible (empty result).
    public async Task<RbacSnapshot> GetEffectiveRbacAsync(CancellationToken cancellationToken = default)
    {
        var profile = _currentUserProfileService.GetCurrentUser();
        if (profile is null)
        {
            return new RbacSnapshot(null, new Dictionary<string, bool>(StringComparer.Ordinal));
        }

        var targetName = profile.DisplayName ?? profile.Username;

        var userRecord = await _userInfoGateway.SelectAsync(profile.UserId, cancellationToken);
        var userValues = ParseRbacValues(userRecord?.MetadataXml);
        if (userValues.Count > 0)
        {
            return new RbacSnapshot(targetName, userValues);
        }

        var systemInfo = await _systemInfoGateway.SelectFirstAsync(cancellationToken);
        var role = NormalizeRoleName(profile.Role);
        var groupValues = ParseRbacValues(systemInfo?.MetadataXml, role);
        if (groupValues.Count > 0)
        {
            return new RbacSnapshot(targetName, groupValues);
        }

        return new RbacSnapshot(targetName, new Dictionary<string, bool>(StringComparer.Ordinal));
    }

    private static string NormalizeRoleName(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return string.Empty;
        }

        var trimmed = role.Trim();
        if (int.TryParse(trimmed, out var numeric))
        {
            return numeric switch
            {
                0 => "Guest",
                1 => "Operator",
                2 => "Supervisor",
                3 => "Manager",
                4 => "Admin",
                _ => trimmed,
            };
        }

        return trimmed.ToLowerInvariant() switch
        {
            "guest" => "Guest",
            "operator" => "Operator",
            "supervisor" => "Supervisor",
            "manager" => "Manager",
            "admin" => "Admin",
            _ => trimmed,
        };
    }

    public async Task SaveGroupRbacAsync(string role, IReadOnlyDictionary<string, bool> values, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required to save group RBAC.", nameof(role));
        }

        var snapshot = await _systemInfoGateway.SelectFirstAsync(cancellationToken);
        var metadataXml = UpsertGroupRbac(snapshot?.MetadataXml, role, values);

        if (snapshot is null)
        {
            await _systemInfoGateway.InsertAsync(new CreateSystemInfoStoredProcedureRequest(
                OwnerName: null,
                MetadataXml: metadataXml), cancellationToken);
            return;
        }

        await _systemInfoGateway.UpdateAsync(new UpdateSystemInfoStoredProcedureRequest(
            SystemId: snapshot.SystemId,
            OwnerName: snapshot.OwnerName,
            MetadataXml: metadataXml), cancellationToken);
    }

    public async Task<RbacSnapshot> GetUserRbacAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var record = await _userInfoGateway.SelectAsync(userId, cancellationToken);
        if (record is null)
        {
            throw new InvalidOperationException($"User {userId} was not found.");
        }

        return new RbacSnapshot(record.UserAlias ?? record.UserName, ParseRbacValues(record.MetadataXml));
    }

    public async Task SaveUserRbacAsync(Guid userId, IReadOnlyDictionary<string, bool> values, CancellationToken cancellationToken = default)
    {
        var record = await _userInfoGateway.SelectAsync(userId, cancellationToken);
        if (record is null)
        {
            throw new InvalidOperationException($"User {userId} was not found.");
        }

        var metadataXml = UpsertRbacEntries(record.MetadataXml, values);

        await _userInfoGateway.UpdateAsync(new UpdateUserInfoStoredProcedureRequest(
            UserId: record.UserId,
            PrimaryRec: record.PrimaryRec,
            UserName: record.UserName,
            UserPassword: record.UserPassword,
            UserAlias: record.UserAlias,
            UserRole: record.UserRole,
            MetadataXml: metadataXml,
            CreatedOn: record.CreatedOn,
            CreatedBy: record.CreatedBy,
            ModifiedOn: DateTime.Now,
            ModifiedBy: record.ModifiedBy,
            Retired: record.Retired,
            RetiredOn: record.RetiredOn ?? default,
            RetiredBy: record.RetiredBy ?? Guid.Empty), cancellationToken);
    }

    internal static IReadOnlyDictionary<string, bool> ParseRbacValues(string? metadataXml, string? role = null)
    {
        var values = new Dictionary<string, bool>();

        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return values;
        }

        try
        {
            var doc = XDocument.Parse(metadataXml);
            var rbacElement = doc.Root?.Descendants(RbacElement).FirstOrDefault();

            if (rbacElement is null)
            {
                return values;
            }

            // When a role is supplied (Group RBAC) the entries live under a
            // role-specific element; otherwise (User RBAC) they are direct children.
            var container = string.IsNullOrWhiteSpace(role)
                ? rbacElement
                : rbacElement.Elements(RoleElement)
                    .FirstOrDefault(r => string.Equals(r.Attribute(RoleAttribute)?.Value, role, StringComparison.OrdinalIgnoreCase));

            // Migration read: pre-role-scoped (flat) Group RBAC is surfaced for the
            // operator role only, so it can be re-saved into its own <Role> element.
            if (container is null && !string.IsNullOrWhiteSpace(role))
            {
                var hasRoleChildren = rbacElement.Elements(RoleElement).Any();
                var hasDirectEntries = rbacElement.Elements(EntryElement).Any();

                if (!hasRoleChildren && hasDirectEntries &&
                    string.Equals(role, OperatorRoleName, StringComparison.OrdinalIgnoreCase))
                {
                    container = rbacElement;
                }
            }

            if (container is null)
            {
                return values;
            }

            foreach (var entry in container.Elements(EntryElement))
            {
                var key = entry.Attribute(KeyAttribute)?.Value;

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                values[key] = string.Equals(entry.Attribute(ValueAttribute)?.Value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // If the XML is malformed, treat it as no stored RBAC values.
        }

        return values;
    }

    internal static string UpsertRbacEntries(string? existingXml, IReadOnlyDictionary<string, bool> values, string? role = null)
    {
        XDocument doc;

        if (string.IsNullOrWhiteSpace(existingXml))
        {
            doc = new XDocument(new XElement(MetadataRoot));
        }
        else
        {
            try
            {
                doc = XDocument.Parse(existingXml);
            }
            catch
            {
                // If existing XML is malformed, start fresh.
                doc = new XDocument(new XElement(MetadataRoot));
            }
        }

        var root = doc.Root!;
        var rbacElement = root.Element(RbacElement);

        if (rbacElement is null)
        {
            rbacElement = new XElement(RbacElement);
            root.Add(rbacElement);
        }

        XElement? container;

        if (string.IsNullOrWhiteSpace(role))
        {
            // User RBAC: entries are direct children of <Rbac>.
            container = rbacElement;
            container.RemoveNodes();
        }
        else
        {
            // Group RBAC: entries are scoped under a <Role name="..."> element,
            // preserving the entries of other roles.
            container = rbacElement.Elements(RoleElement)
                .FirstOrDefault(r => string.Equals(r.Attribute(RoleAttribute)?.Value, role, StringComparison.OrdinalIgnoreCase));

            if (container is null)
            {
                container = new XElement(RoleElement, new XAttribute(RoleAttribute, role));
                rbacElement.Add(container);
            }
            else
            {
                container.RemoveNodes();
            }
        }

        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            container.Add(new XElement(EntryElement,
                new XAttribute(KeyAttribute, pair.Key),
                new XAttribute(ValueAttribute, pair.Value ? "true" : "false")));
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    // Group RBAC was previously stored as a single flat blob (entries directly
    // under <Rbac>). This migrates that legacy data into a <Role name="operator">
    // element and then applies the requested role's values. The legacy flat
    // entries are consumed so they are not duplicated.
    internal static string UpsertGroupRbac(string? existingXml, string role, IReadOnlyDictionary<string, bool> values)
    {
        XDocument doc;

        if (string.IsNullOrWhiteSpace(existingXml))
        {
            doc = new XDocument(new XElement(MetadataRoot));
        }
        else
        {
            try
            {
                doc = XDocument.Parse(existingXml);
            }
            catch
            {
                doc = new XDocument(new XElement(MetadataRoot));
            }
        }

        var root = doc.Root!;
        var rbacElement = root.Element(RbacElement);

        if (rbacElement is null)
        {
            rbacElement = new XElement(RbacElement);
            root.Add(rbacElement);
        }

        var legacyEntries = rbacElement.Elements(EntryElement).ToList();
        var hasRoleChildren = rbacElement.Elements(RoleElement).Any();
        var isLegacy = !hasRoleChildren && legacyEntries.Count > 0;

        if (isLegacy)
        {
            var legacyValues = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var entry in legacyEntries)
            {
                var key = entry.Attribute(KeyAttribute)?.Value;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                legacyValues[key] = string.Equals(
                    entry.Attribute(ValueAttribute)?.Value, "true", StringComparison.OrdinalIgnoreCase);
            }

            legacyEntries.Remove();

            if (string.Equals(role, OperatorRoleName, StringComparison.OrdinalIgnoreCase))
            {
                var merged = new Dictionary<string, bool>(legacyValues, StringComparer.Ordinal);
                foreach (var pair in values)
                {
                    merged[pair.Key] = pair.Value;
                }

                WriteRoleContainer(rbacElement, role, merged);
            }
            else
            {
                WriteRoleContainer(rbacElement, OperatorRoleName, legacyValues);
                WriteRoleContainer(rbacElement, role, values);
            }
        }
        else
        {
            WriteRoleContainer(rbacElement, role, values);
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static void WriteRoleContainer(
        XElement rbacElement,
        string role,
        IReadOnlyDictionary<string, bool> values)
    {
        var container = rbacElement.Elements(RoleElement)
            .FirstOrDefault(r => string.Equals(r.Attribute(RoleAttribute)?.Value, role, StringComparison.OrdinalIgnoreCase));

        if (container is null)
        {
            container = new XElement(RoleElement, new XAttribute(RoleAttribute, role));
            rbacElement.Add(container);
        }
        else
        {
            container.RemoveNodes();
        }

        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            container.Add(new XElement(EntryElement,
                new XAttribute(KeyAttribute, pair.Key),
                new XAttribute(ValueAttribute, pair.Value ? "true" : "false")));
        }
    }
}
