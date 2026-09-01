using System.Xml.Linq;

namespace JB2026.Api.Services;

public static class MetadataXmlHelper
{
    private static readonly XNamespace Empty = XNamespace.None;

    public static bool ExtractTwoFactorEnabled(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return false;
        try
        {
            var xml = XDocument.Parse(metadataXml).Root!;
            var enabled = xml.Element("TwoFactor")?.Element("Enabled")?.Value;
            return enabled == "true";
        }
        catch
        {
            return false;
        }
    }

    public static string ExtractTwoFactorSecret(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;
        try
        {
            var xml = XDocument.Parse(metadataXml).Root!;
            return xml.Element("TwoFactor")?.Element("Secret")?.Value?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string ExtractTwoFactorRecoveryCodes(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
            return string.Empty;
        try
        {
            var xml = XDocument.Parse(metadataXml).Root!;
            return xml.Element("TwoFactor")?.Element("RecoveryCodes")?.Value?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string SetTwoFactorInMetadata(string? metadataXml, bool enabled, string secret, string recoveryCodes)
    {
        XElement xml;
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            xml = new XElement("Metadata");
        }
        else
        {
            try
            {
                xml = XDocument.Parse(metadataXml).Root!;
            }
            catch
            {
                xml = new XElement("Metadata");
            }
        }

        var twoFactorEl = xml.Element("TwoFactor");
        if (twoFactorEl is null)
        {
            twoFactorEl = new XElement("TwoFactor");
            xml.Add(twoFactorEl);
        }

        SetOrCreateElement(twoFactorEl, "Enabled", enabled.ToString().ToLowerInvariant());
        SetOrCreateElement(twoFactorEl, "Secret", secret);
        SetOrCreateElement(twoFactorEl, "RecoveryCodes", recoveryCodes);

        return xml.ToString();
    }

    public static string SetTwoFactorEnabledInMetadata(string? metadataXml, bool enabled)
    {
        XElement xml;
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            xml = new XElement("Metadata");
        }
        else
        {
            try
            {
                xml = XDocument.Parse(metadataXml).Root!;
            }
            catch
            {
                xml = new XElement("Metadata");
            }
        }

        var twoFactorEl = xml.Element("TwoFactor");
        if (twoFactorEl is null)
        {
            twoFactorEl = new XElement("TwoFactor");
            xml.Add(twoFactorEl);
        }

        SetOrCreateElement(twoFactorEl, "Enabled", enabled.ToString().ToLowerInvariant());

        return xml.ToString();
    }

    private static void SetOrCreateElement(XElement parent, string name, string value)
    {
        var el = parent.Element(name);
        if (el is null)
        {
            parent.Add(new XElement(name, value));
        }
        else
        {
            el.Value = value;
        }
    }
}
