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

        string? persistedNextOrderNumber = null;
        string? persistedNextProductNumber = null;
        string? persistedNextQuotationNumber = null;
        string? persistedDateFormat = null;

        if (snapshot?.MetadataXml is not null)
        {
            try
            {
                var doc = XDocument.Parse(snapshot.MetadataXml);

                // Try new format first: <Settings .../>
                var settingsElement = doc.Root?.Descendants(SettingsElement).FirstOrDefault();

                if (settingsElement is not null)
                {
                    persistedNextOrderNumber = settingsElement.Attribute(NextOrderNumberAttr)?.Value;
                    persistedNextProductNumber = settingsElement.Attribute(NextProductNumberAttr)?.Value;
                    persistedNextQuotationNumber = settingsElement.Attribute(NextQuotationNumberAttr)?.Value;
                    persistedDateFormat = settingsElement.Attribute("DateFormatPreference")?.Value;
                }
                else
                {
                    // Fall back to old format: <record id="data" .../>
                    var dataRecord = doc.Root?.Elements(RecordElement)
                        .FirstOrDefault(el => el.Attribute(RecordIdAttribute)?.Value == DataRecordId);

                    if (dataRecord is not null)
                    {
                        persistedNextOrderNumber = dataRecord.Attribute(NextOrderNumberAttr)?.Value;
                        persistedNextProductNumber = dataRecord.Attribute(NextProductNumberAttr)?.Value;
                        persistedNextQuotationNumber = dataRecord.Attribute(NextQuotationNumberAttr)?.Value;
                        persistedDateFormat = dataRecord.Attribute("dateFormatPreference")?.Value;
                    }
                }
            }
            catch
            {
                // If XML is malformed, fall back to in-memory defaults
            }
        }

        return new SettingsResponse
        {
            CompanyName = baseSettings.CompanyName,
            TimeZone = baseSettings.TimeZone,
            CurrencyCode = baseSettings.CurrencyCode,
            EnableLegacyFallback = baseSettings.EnableLegacyFallback,
            OwnerName = string.IsNullOrWhiteSpace(snapshot?.OwnerName) ? baseSettings.OwnerName : snapshot.OwnerName,
            NextOrderNumber = persistedNextOrderNumber ?? baseSettings.NextOrderNumber,
            NextProductNumber = persistedNextProductNumber ?? baseSettings.NextProductNumber,
            NextQuotationNumber = persistedNextQuotationNumber ?? baseSettings.NextQuotationNumber,
            CommonQueryIndex = baseSettings.CommonQueryIndex,
            CompletedQueryIndex = baseSettings.CompletedQueryIndex,
            ScheduleQueryRange = baseSettings.ScheduleQueryRange,
            GmailAccount = baseSettings.GmailAccount,
            GmailPassword = baseSettings.GmailPassword,
            DateFormatPreference = persistedDateFormat ?? baseSettings.DateFormatPreference,
        };
    }

    public SettingsResponse Update(UpdateSettingsRequest request)
    {
        var updated = _fallback.Update(request);
        var snapshot = GetSystemInfoSnapshot();
        var metadataXml = UpsertSettingsAttributes(
            snapshot?.MetadataXml, 
            updated.NextOrderNumber,
            updated.NextProductNumber,
            updated.NextQuotationNumber,
            updated.DateFormatPreference);

        if (snapshot is null)
        {
            _gateway.InsertAsync(new CreateSystemInfoStoredProcedureRequest(
                OwnerName: updated.OwnerName,
                MetadataXml: metadataXml)).GetAwaiter().GetResult();

            return updated;
        }

        _gateway.UpdateAsync(new UpdateSystemInfoStoredProcedureRequest(
            SystemId: snapshot.SystemId,
            OwnerName: updated.OwnerName,
            MetadataXml: metadataXml)).GetAwaiter().GetResult();

        return updated;
    }

    private static string UpsertSettingsAttributes(
        string? existingXml,
        string nextOrderNumber,
        string nextProductNumber,
        string nextQuotationNumber,
        string dateFormatPreference)
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
            }
        }
        else
        {
            // Update existing Settings element
            settingsElement.SetAttributeValue(NextOrderNumberAttr, nextOrderNumber);
            settingsElement.SetAttributeValue(NextProductNumberAttr, nextProductNumber);
            settingsElement.SetAttributeValue(NextQuotationNumberAttr, nextQuotationNumber);
            settingsElement.SetAttributeValue("DateFormatPreference", dateFormatPreference);
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
