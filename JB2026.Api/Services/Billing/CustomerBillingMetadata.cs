namespace JB2026.Api.Services.Billing;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

/// <summary>
/// Metadata for Invoice Ninja integration stored in the Customer record.
/// Persisted in the Customer.MetadataXml field for idempotent synchronization.
/// </summary>
public class CustomerBillingMetadata
{
    /// <summary>
    /// Invoice Ninja client ID. Used for idempotent updates and invoice generation.
    /// </summary>
    public string? InvoiceNinjaClientId { get; set; }

    /// <summary>
    /// Timestamp of the last successful sync with Invoice Ninja (UTC).
    /// </summary>
    public DateTime? InvoiceNinjaClientSyncedAt { get; set; }

    /// <summary>
    /// Status of the last sync attempt (e.g., "success", "failed").
    /// </summary>
    public string? InvoiceNinjaClientSyncStatus { get; set; }

    /// <summary>
    /// Error message from the last failed sync (if applicable).
    /// </summary>
    public string? InvoiceNinjaClientSyncError { get; set; }
}

/// <summary>
/// Helper for reading/writing billing metadata from/to Customer.MetadataXml.
/// </summary>
public static class CustomerBillingMetadataHelper
{
    private const string MetadataRootElementName = "Metadata";
    private const string InvoiceNinjaClientIdElementName = "invoiceNinjaClientId";
    private const string InvoiceNinjaClientSyncedAtElementName = "invoiceNinjaClientSyncedAt";
    private const string InvoiceNinjaClientSyncStatusElementName = "invoiceNinjaClientSyncStatus";
    private const string InvoiceNinjaClientSyncErrorElementName = "invoiceNinjaClientSyncError";

    /// <summary>
    /// Extracts Invoice Ninja billing metadata from the Customer MetadataXml.
    /// </summary>
    /// <param name="metadataXml">The Customer.MetadataXml value (can be null or empty).</param>
    /// <returns>Billing metadata with available values; empty metadata if XML is absent or malformed.</returns>
    public static CustomerBillingMetadata ExtractBillingMetadata(string? metadataXml)
    {
        var metadata = new CustomerBillingMetadata();

        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return metadata;
        }

        if (TryExtractBillingMetadataFromJson(metadataXml, out var jsonMetadata))
        {
            return jsonMetadata;
        }

        try
        {
            var doc = XDocument.Parse(metadataXml);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != MetadataRootElementName)
            {
                return metadata;
            }

            var clientIdElement = root.Element(InvoiceNinjaClientIdElementName);
            if (clientIdElement?.Value is not null)
            {
                metadata.InvoiceNinjaClientId = clientIdElement.Value;
            }

            var syncedAtElement = root.Element(InvoiceNinjaClientSyncedAtElementName);
            if (syncedAtElement?.Value is not null && DateTime.TryParseExact(
                syncedAtElement.Value,
                "O", // ISO 8601 format
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var syncedAt))
            {
                metadata.InvoiceNinjaClientSyncedAt = syncedAt;
            }

            var syncStatusElement = root.Element(InvoiceNinjaClientSyncStatusElementName);
            if (syncStatusElement?.Value is not null)
            {
                metadata.InvoiceNinjaClientSyncStatus = syncStatusElement.Value;
            }

            var syncErrorElement = root.Element(InvoiceNinjaClientSyncErrorElementName);
            if (syncErrorElement?.Value is not null)
            {
                metadata.InvoiceNinjaClientSyncError = syncErrorElement.Value;
            }

            return metadata;
        }
        catch (Exception)
        {
            // If XML parsing fails, return empty metadata
            return metadata;
        }
    }

    /// <summary>
    /// Merges billing metadata into the Customer MetadataXml, preserving other metadata.
    /// </summary>
    /// <param name="metadataXml">The existing Customer.MetadataXml (can be null or empty).</param>
    /// <param name="billingMetadata">The billing metadata to merge.</param>
    /// <returns>Updated MetadataXml with billing metadata merged in.</returns>
    public static string MergeBillingMetadata(string? metadataXml, CustomerBillingMetadata billingMetadata)
    {
        if (TryParseMetadataJsonObject(metadataXml, out var jsonRoot))
        {
            MergeBillingMetadataIntoJson(jsonRoot!, billingMetadata);
            return jsonRoot!.ToJsonString();
        }

        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            var newJsonRoot = new JsonObject();
            MergeBillingMetadataIntoJson(newJsonRoot, billingMetadata);
            return newJsonRoot.ToJsonString();
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(metadataXml);
            if (doc.Root == null || doc.Root.Name.LocalName != MetadataRootElementName)
            {
                doc = new XDocument(new XElement(MetadataRootElementName));
            }
        }
        catch (Exception)
        {
            // Keep modern JSON format when legacy XML parsing fails.
            var fallbackJsonRoot = new JsonObject();
            MergeBillingMetadataIntoJson(fallbackJsonRoot, billingMetadata);
            return fallbackJsonRoot.ToJsonString();
        }

        var root = doc.Root;
        if (root == null)
        {
            throw new InvalidOperationException("Failed to create metadata XML structure.");
        }

        // Update or remove Invoice Ninja client ID
        var clientIdElement = root.Element(InvoiceNinjaClientIdElementName);
        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientId))
        {
            if (clientIdElement != null)
            {
                clientIdElement.Value = billingMetadata.InvoiceNinjaClientId;
            }
            else
            {
                root.Add(new XElement(InvoiceNinjaClientIdElementName, billingMetadata.InvoiceNinjaClientId));
            }
        }
        else if (clientIdElement != null)
        {
            clientIdElement.Remove();
        }

        // Update or remove sync timestamp
        var syncedAtElement = root.Element(InvoiceNinjaClientSyncedAtElementName);
        if (billingMetadata.InvoiceNinjaClientSyncedAt.HasValue)
        {
            var isoString = billingMetadata.InvoiceNinjaClientSyncedAt.Value.ToString("O");
            if (syncedAtElement != null)
            {
                syncedAtElement.Value = isoString;
            }
            else
            {
                root.Add(new XElement(InvoiceNinjaClientSyncedAtElementName, isoString));
            }
        }
        else if (syncedAtElement != null)
        {
            syncedAtElement.Remove();
        }

        // Update or remove sync status
        var syncStatusElement = root.Element(InvoiceNinjaClientSyncStatusElementName);
        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientSyncStatus))
        {
            if (syncStatusElement != null)
            {
                syncStatusElement.Value = billingMetadata.InvoiceNinjaClientSyncStatus;
            }
            else
            {
                root.Add(new XElement(InvoiceNinjaClientSyncStatusElementName, billingMetadata.InvoiceNinjaClientSyncStatus));
            }
        }
        else if (syncStatusElement != null)
        {
            syncStatusElement.Remove();
        }

        // Update or remove sync error
        var syncErrorElement = root.Element(InvoiceNinjaClientSyncErrorElementName);
        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientSyncError))
        {
            if (syncErrorElement != null)
            {
                syncErrorElement.Value = billingMetadata.InvoiceNinjaClientSyncError;
            }
            else
            {
                root.Add(new XElement(InvoiceNinjaClientSyncErrorElementName, billingMetadata.InvoiceNinjaClientSyncError));
            }
        }
        else if (syncErrorElement != null)
        {
            syncErrorElement.Remove();
        }

        return doc.ToString();
    }

    private static bool TryExtractBillingMetadataFromJson(string metadataJson, out CustomerBillingMetadata metadata)
    {
        metadata = new CustomerBillingMetadata();

        if (!TryParseMetadataJsonObject(metadataJson, out var root))
        {
            return false;
        }

        metadata.InvoiceNinjaClientId = TryGetJsonString(root!, InvoiceNinjaClientIdElementName);
        metadata.InvoiceNinjaClientSyncStatus = TryGetJsonString(root!, InvoiceNinjaClientSyncStatusElementName);
        metadata.InvoiceNinjaClientSyncError = TryGetJsonString(root!, InvoiceNinjaClientSyncErrorElementName);

        var syncedAtRaw = TryGetJsonString(root!, InvoiceNinjaClientSyncedAtElementName);
        if (!string.IsNullOrWhiteSpace(syncedAtRaw)
            && DateTime.TryParseExact(syncedAtRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var syncedAt))
        {
            metadata.InvoiceNinjaClientSyncedAt = syncedAt;
        }

        return true;
    }

    private static bool TryParseMetadataJsonObject(string? raw, out JsonObject? root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            var parsed = JsonNode.Parse(raw.Trim()) as JsonObject;
            if (parsed is null)
            {
                return false;
            }

            root = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryGetJsonString(JsonObject root, string propertyName)
    {
        foreach (var property in root)
        {
            if (!string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
            {
                return stringValue;
            }

            if (property.Value is not null)
            {
                return property.Value.ToString();
            }
        }

        return null;
    }

    private static void MergeBillingMetadataIntoJson(JsonObject root, CustomerBillingMetadata billingMetadata)
    {
        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientId))
        {
            root[InvoiceNinjaClientIdElementName] = billingMetadata.InvoiceNinjaClientId;
        }
        else
        {
            root.Remove(InvoiceNinjaClientIdElementName);
        }

        if (billingMetadata.InvoiceNinjaClientSyncedAt.HasValue)
        {
            root[InvoiceNinjaClientSyncedAtElementName] = billingMetadata.InvoiceNinjaClientSyncedAt.Value.ToString("O", CultureInfo.InvariantCulture);
        }
        else
        {
            root.Remove(InvoiceNinjaClientSyncedAtElementName);
        }

        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientSyncStatus))
        {
            root[InvoiceNinjaClientSyncStatusElementName] = billingMetadata.InvoiceNinjaClientSyncStatus;
        }
        else
        {
            root.Remove(InvoiceNinjaClientSyncStatusElementName);
        }

        if (!string.IsNullOrWhiteSpace(billingMetadata.InvoiceNinjaClientSyncError))
        {
            root[InvoiceNinjaClientSyncErrorElementName] = billingMetadata.InvoiceNinjaClientSyncError;
        }
        else
        {
            root.Remove(InvoiceNinjaClientSyncErrorElementName);
        }
    }

    /// <summary>
    /// Marks a billing sync as successful and updates the metadata.
    /// </summary>
    public static CustomerBillingMetadata MarkSyncSuccessful(
        string? metadataXml,
        string invoiceNinjaClientId)
    {
        var existing = ExtractBillingMetadata(metadataXml);
        var updated = new CustomerBillingMetadata
        {
            InvoiceNinjaClientId = invoiceNinjaClientId,
            InvoiceNinjaClientSyncedAt = DateTime.UtcNow,
            InvoiceNinjaClientSyncStatus = "success"
        };

        return updated;
    }

    /// <summary>
    /// Marks a billing sync as failed and updates the metadata with error information.
    /// </summary>
    public static CustomerBillingMetadata MarkSyncFailed(
        string? metadataXml,
        string errorMessage)
    {
        var existing = ExtractBillingMetadata(metadataXml);
        var updated = new CustomerBillingMetadata
        {
            InvoiceNinjaClientId = existing.InvoiceNinjaClientId, // Preserve existing ID
            InvoiceNinjaClientSyncedAt = DateTime.UtcNow,
            InvoiceNinjaClientSyncStatus = "failed",
            InvoiceNinjaClientSyncError = errorMessage
        };

        return updated;
    }
}
