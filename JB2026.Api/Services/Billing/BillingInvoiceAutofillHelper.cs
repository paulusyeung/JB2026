namespace JB2026.Api.Services.Billing;

using System.Net;
using System.Text.RegularExpressions;

public static partial class BillingInvoiceAutofillHelper
{
    public static string BuildCanonicalLookupKey(string? orderNumber, int jobSuffix)
    {
        return $"{NormalizeOrderNumber(orderNumber)}-{jobSuffix}";
    }

    public static string NormalizeOrderNumber(string? orderNumber)
    {
        return string.IsNullOrWhiteSpace(orderNumber)
            ? string.Empty
            : orderNumber.Trim();
    }

    public static string SanitizeForJson(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var buffer = new char[value.Length];
        var index = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (!char.IsSurrogate(current))
            {
                buffer[index++] = current;
                continue;
            }

            if (char.IsHighSurrogate(current)
                && i + 1 < value.Length
                && char.IsLowSurrogate(value[i + 1]))
            {
                buffer[index++] = current;
                buffer[index++] = value[++i];
                continue;
            }

            buffer[index++] = '\uFFFD';
        }

        return new string(buffer, 0, index);
    }

    public static bool TryParseCanonicalJobNumber(string? input, out CanonicalJobReference? reference)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        var separatorIndex = trimmed.LastIndexOf('-');
        if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
        {
            return false;
        }

        var orderNumber = trimmed[..separatorIndex].Trim();
        var suffixText = trimmed[(separatorIndex + 1)..].Trim();
        if (orderNumber.Length == 0 || !int.TryParse(suffixText, out var jobSuffix) || jobSuffix <= 0)
        {
            return false;
        }

        reference = new CanonicalJobReference(trimmed, NormalizeOrderNumber(orderNumber), jobSuffix);
        return true;
    }

    public static string NormalizeProductDetailsPlainText(string? productDetails)
    {
        if (string.IsNullOrWhiteSpace(productDetails))
        {
            return string.Empty;
        }

        var normalized = BreakTagRegex().Replace(productDetails, "\n");
        normalized = ListItemOpenRegex().Replace(normalized, "- ");
        normalized = HtmlTagRegex().Replace(normalized, string.Empty);
        normalized = WebUtility.HtmlDecode(normalized)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u00A0', ' ');

        var lines = normalized
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();

        var collapsed = new List<string>();
        var previousBlank = false;
        foreach (var line in lines)
        {
            var isBlank = string.IsNullOrWhiteSpace(line);
            if (isBlank)
            {
                if (!previousBlank)
                {
                    collapsed.Add(string.Empty);
                }

                previousBlank = true;
                continue;
            }

            collapsed.Add(line);
            previousBlank = false;
        }

        return string.Join("\n", collapsed).Trim();
    }

    public static string? ExtractSectionOneDescription(string? productDetails)
    {
        var normalized = NormalizeProductDetailsPlainText(productDetails);
        if (normalized.Length == 0)
        {
            return null;
        }

        var lines = normalized.Split('\n');
        var sectionStart = Array.FindIndex(lines, line => SectionOneRegex().IsMatch(line));
        if (sectionStart < 0)
        {
            return null;
        }

        var bodyLines = new List<string>();
        for (var index = sectionStart + 1; index < lines.Length; index++)
        {
            var line = lines[index];
            if (AnySectionHeaderRegex().IsMatch(line))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            bodyLines.Add(line);
        }

        while (bodyLines.Count > 0 && bodyLines[0].Length == 0)
        {
            bodyLines.RemoveAt(0);
        }

        while (bodyLines.Count > 0 && bodyLines[^1].Length == 0)
        {
            bodyLines.RemoveAt(bodyLines.Count - 1);
        }

        return bodyLines.Count == 0 ? null : string.Join("\n", bodyLines);
    }

    [GeneratedRegex("(?i)<br\\s*/?>|</p\\s*>|</div\\s*>|</li\\s*>")]
    private static partial Regex BreakTagRegex();

    [GeneratedRegex("(?i)<li\\s*>")]
    private static partial Regex ListItemOpenRegex();

    [GeneratedRegex("(?is)<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("^\\s*1\\.\\s*")]
    private static partial Regex SectionOneRegex();

    [GeneratedRegex("^\\s*\\d+\\.\\s*")]
    private static partial Regex AnySectionHeaderRegex();
}

public sealed record CanonicalJobReference(string CanonicalJobNumber, string OrderNumber, int JobSuffix);