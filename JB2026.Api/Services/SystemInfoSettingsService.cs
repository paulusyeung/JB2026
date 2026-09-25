namespace JB2026.Api.Services;

using System.Xml.Linq;
using JB2026.Api.Models;

public sealed class SystemInfoSettingsService : ISettingsService
{
    private const string MetadataRoot = "Metadata";
    private const string SettingsElement = "Settings";
    private const string RecordElement = "record";
    private const string RecordIdAttribute = "id";
    private const string DataRecordId = "data";
    private const string NextOrderNumberAttr = "NextOrderNumber";
    private const string NextProductNumberAttr = "NextProductNumber";
    private const string NextQuotationNumberAttr = "NextQuotationNumber";

    private readonly ISystemInfoStoredProcedureGateway _gateway;
    private readonly InMemorySettingsService _fallback;

    public SystemInfoSettingsService(
        ISystemInfoStoredProcedureGateway gateway,
        InMemorySettingsService fallback)
    {
        _gateway = gateway;
        _fallback = fallback;
    }

    public SettingsResponse Get()
    {
        var baseSettings = _fallback.Get();
        var snapshot = GetSystemInfoSnapshot();
        var persisted = ParsePersistedSettings(snapshot?.MetadataXml);

        return new SettingsResponse
        {
            CompanyName = baseSettings.CompanyName,
            TimeZone = baseSettings.TimeZone,
            CurrencyCode = baseSettings.CurrencyCode,
            EnableLegacyFallback = baseSettings.EnableLegacyFallback,
            OwnerName = string.IsNullOrWhiteSpace(snapshot?.OwnerName) ? baseSettings.OwnerName : snapshot.OwnerName,
            NextOrderNumber = persisted.NextOrderNumber ?? baseSettings.NextOrderNumber,
            NextProductNumber = persisted.NextProductNumber ?? baseSettings.NextProductNumber,
            NextQuotationNumber = persisted.NextQuotationNumber ?? baseSettings.NextQuotationNumber,
            CommonQueryIndex = baseSettings.CommonQueryIndex,
            CompletedQueryIndex = baseSettings.CompletedQueryIndex,
            ScheduleQueryRange = baseSettings.ScheduleQueryRange,
            GmailAccount = baseSettings.GmailAccount,
            GmailPassword = baseSettings.GmailPassword,
            DateFormatPreference = persisted.DateFormatPreference ?? baseSettings.DateFormatPreference,
            JobListDaysBack = int.TryParse(persisted.JobListDaysBack, out var daysBack) ? daysBack : baseSettings.JobListDaysBack,
        };
    }

    private static PersistedSettings ParsePersistedSettings(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return new PersistedSettings(null, null, null, null, null);
        }

        try
        {
            var doc = XDocument.Parse(metadataXml);

            // Try new format first: <Settings .../>
            var settingsElement = doc.Root?.Descendants(SettingsElement).FirstOrDefault();

            if (settingsElement is not null)
            {
                return new PersistedSettings(
                    settingsElement.Attribute(NextOrderNumberAttr)?.Value,
                    settingsElement.Attribute(NextProductNumberAttr)?.Value,
                    settingsElement.Attribute(NextQuotationNumberAttr)?.Value,
                    settingsElement.Attribute("DateFormatPreference")?.Value,
                    settingsElement.Attribute("JobListDaysBack")?.Value);
            }

            // Fall back to old format: <record id="data" .../>
            var dataRecord = doc.Root?.Elements(RecordElement)
                .FirstOrDefault(el => el.Attribute(RecordIdAttribute)?.Value == DataRecordId);

            if (dataRecord is not null)
            {
                return new PersistedSettings(
                    dataRecord.Attribute(NextOrderNumberAttr)?.Value,
                    dataRecord.Attribute(NextProductNumberAttr)?.Value,
                    dataRecord.Attribute(NextQuotationNumberAttr)?.Value,
                    dataRecord.Attribute("dateFormatPreference")?.Value,
                    dataRecord.Attribute("JobListDaysBack")?.Value);
            }
        }
        catch
        {
            // If XML is malformed, fall back to in-memory defaults
        }

        return new PersistedSettings(null, null, null, null, null);
    }

    private sealed record PersistedSettings(
        string? NextOrderNumber,
        string? NextProductNumber,
        string? NextQuotationNumber,
        string? DateFormatPreference,
        string? JobListDaysBack);

    public SettingsResponse Update(UpdateSettingsRequest request)
    {
        var updated = _fallback.Update(request);

        // Routed through the gateway so a concurrent order-number allocation cannot be lost.
        _gateway.MutateMetadataAsync(
            currentXml => UpsertSettingsAttributes(
                currentXml,
                updated.NextOrderNumber,
                updated.NextProductNumber,
                updated.NextQuotationNumber,
                updated.DateFormatPreference,
                updated.JobListDaysBack),
            CancellationToken.None).GetAwaiter().GetResult();

        return updated;
    }

    public async Task<string> AllocateNextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var baseSettings = _fallback.Get();
        string? allocated = null;

        await _gateway.MutateMetadataAsync(
            currentXml =>
            {
                var persisted = ParsePersistedSettings(currentXml);
                allocated = persisted.NextOrderNumber ?? baseSettings.NextOrderNumber;

                return UpsertSettingsAttributes(
                    currentXml,
                    InMemorySettingsService.IncrementOrderNumber(allocated),
                    persisted.NextProductNumber ?? baseSettings.NextProductNumber,
                    persisted.NextQuotationNumber ?? baseSettings.NextQuotationNumber,
                    persisted.DateFormatPreference ?? baseSettings.DateFormatPreference,
                    int.TryParse(persisted.JobListDaysBack, out var daysBack) ? daysBack : baseSettings.JobListDaysBack);
            },
            cancellationToken);

        if (allocated is null)
        {
            throw new InvalidOperationException("Cannot allocate an order number because no SystemInfo record exists.");
        }

        return allocated;
    }

    private static string UpsertSettingsAttributes(
        string? existingXml,
        string nextOrderNumber,
        string nextProductNumber,
        string nextQuotationNumber,
        string dateFormatPreference,
        int jobListDaysBack)
    {
        XDocument doc;

        if (string.IsNullOrWhiteSpace(existingXml))
        {
            doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(MetadataRoot));
        }
        else
        {
            try
            {
                doc = XDocument.Parse(existingXml);
            }
            catch
            {
                // If existing XML is malformed, start fresh
                doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(MetadataRoot));
            }
        }

        var root = doc.Root!;
        
        // Try new format first: <Settings .../>
        var settingsElement = root.Element(SettingsElement);

        if (settingsElement is null)
        {
            // Check for old format: <record id="data" .../>
            var dataRecord = root.Elements(RecordElement)
                .FirstOrDefault(el => el.Attribute(RecordIdAttribute)?.Value == DataRecordId);

            if (dataRecord is not null)
            {
                // Update existing record element with new attributes
                dataRecord.SetAttributeValue(NextOrderNumberAttr, nextOrderNumber);
                dataRecord.SetAttributeValue(NextProductNumberAttr, nextProductNumber);
                dataRecord.SetAttributeValue(NextQuotationNumberAttr, nextQuotationNumber);
                dataRecord.SetAttributeValue("DateFormatPreference", dateFormatPreference);
                dataRecord.SetAttributeValue("JobListDaysBack", jobListDaysBack);
            }
            else
            {
                // Create new Settings element
                settingsElement = new XElement(SettingsElement);
                root.Add(settingsElement);
                
                settingsElement.SetAttributeValue(NextOrderNumberAttr, nextOrderNumber);
                settingsElement.SetAttributeValue(NextProductNumberAttr, nextProductNumber);
                settingsElement.SetAttributeValue(NextQuotationNumberAttr, nextQuotationNumber);
                settingsElement.SetAttributeValue("DateFormatPreference", dateFormatPreference);
                settingsElement.SetAttributeValue("JobListDaysBack", jobListDaysBack);
            }
        }
        else
        {
            // Update existing Settings element
            settingsElement.SetAttributeValue(NextOrderNumberAttr, nextOrderNumber);
            settingsElement.SetAttributeValue(NextProductNumberAttr, nextProductNumber);
            settingsElement.SetAttributeValue(NextQuotationNumberAttr, nextQuotationNumber);
            settingsElement.SetAttributeValue("DateFormatPreference", dateFormatPreference);
            settingsElement.SetAttributeValue("JobListDaysBack", jobListDaysBack);
        }

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private SystemInfoStoredProcedureRecord? GetSystemInfoSnapshot()
    {
        try
        {
            return _gateway.SelectFirstAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // If lookup fails, treat as no system info record (will fall back to in-memory defaults)
            return null;
        }
    }
}
