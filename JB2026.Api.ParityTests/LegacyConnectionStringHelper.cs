using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace JB2026.Api.ParityTests;

internal static class LegacyConnectionStringHelper
{
    public static string ResolveLegacyProviderConnectionString()
    {
        const string appConfigPath = @"C:\Projects\JB2015\JB5.EF6\App.Config";
        var document = XDocument.Load(appConfigPath);
        var entityConnection = document
            .Descendants("add")
            .First(node => string.Equals((string?)node.Attribute("name"), "JB5Entities", StringComparison.OrdinalIgnoreCase))
            .Attribute("connectionString")
            ?.Value
            ?? throw new InvalidOperationException("JB5Entities connection string not found.");

        var match = Regex.Match(
            entityConnection,
            "provider connection string=\"(?<provider>.+?)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            throw new InvalidOperationException("Provider connection string marker not found.");
        }

        var providerConnection = match.Groups["provider"].Value;
        if (!providerConnection.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
        {
            providerConnection = providerConnection.TrimEnd(';') + ";TrustServerCertificate=True;Encrypt=False";
        }

        return providerConnection;
    }
}
