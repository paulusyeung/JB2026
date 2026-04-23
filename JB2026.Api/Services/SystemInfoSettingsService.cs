using System.Xml.Linq;
using JB2026.Api.Models;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SystemInfoSettingsService : ISettingsService
{
    private const string MetadataRoot = "Metadata";
    private const string MetadataRecordElement = "record";
    private const string MetadataRecordIdAttribute = "id";
    private const string MetadataDataRecordId = "data";
    private const string DateFormatPreferenceAttribute = "dateFormatPreference";

    private static readonly HashSet<string> SupportedDateFormats = new(StringComparer.Ordinal)
    {
        "shortDate",
        "shortDateTime",
        "shortTime",
        "longDate",
        "longDateTime",
        "custom",
        "isoDate",
        "isoDateTime",
    };

    private readonly InMemorySettingsService _fallback;
    private readonly ISystemInfoStoredProcedureGateway _gateway;
    private readonly JB5LegacyReadContext _readContext;

    public SystemInfoSettingsService(
        InMemorySettingsService fallback,
        ISystemInfoStoredProcedureGateway gateway,
        JB5LegacyReadContext readContext)
    {
        _fallback = fallback;
        _gateway = gateway;
        _readContext = readContext;
    }

    public SettingsResponse Get()
    {
        var baseSettings = _fallback.Get();
        var systemInfo = GetSystemInfoSnapshot();
        if (systemInfo is null)
        {
            return baseSettings;
        }

        var persistedDateFormat = ExtractDateFormatPreference(systemInfo.MetadataXml);
        return new SettingsResponse
        {
            CompanyName = baseSettings.CompanyName,
            TimeZone = baseSettings.TimeZone,
            CurrencyCode = baseSettings.CurrencyCode,
            EnableLegacyFallback = baseSettings.EnableLegacyFallback,
            OwnerName = string.IsNullOrWhiteSpace(systemInfo.OwnerName) ? baseSettings.OwnerName : systemInfo.OwnerName,
            NextOrderNumber = baseSettings.NextOrderNumber,
            NextProductNumber = baseSettings.NextProductNumber,
            NextQuotationNumber = baseSettings.NextQuotationNumber,
            CommonQueryIndex = baseSettings.CommonQueryIndex,
            CompletedQueryIndex = baseSettings.CompletedQueryIndex,
            ScheduleQueryRange = baseSettings.ScheduleQueryRange,
            GmailAccount = baseSettings.GmailAccount,
            GmailPassword = baseSettings.GmailPassword,
            DateFormatPreference = persistedDateFormat,
        };
    }

    public SettingsResponse Update(UpdateSettingsRequest request)
    {
        var updated = _fallback.Update(request);
        var snapshot = GetSystemInfoSnapshot();
        var metadataXml = UpsertDateFormatPreference(snapshot?.MetadataXml, updated.DateFormatPreference);

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

    private SystemInfoSnapshot? GetSystemInfoSnapshot()
    {
        return _readContext.SystemInfos
            .AsNoTracking()
            .OrderBy(systemInfo => systemInfo.SystemId)
            .Select(systemInfo => new SystemInfoSnapshot(
                systemInfo.SystemId,
                systemInfo.OwnerName,
                systemInfo.MetadataXml))
            .FirstOrDefault();
    }

    private static string ExtractDateFormatPreference(string? metadataXml)
    {
        var metadata = ParseMetadata(metadataXml);
        if (metadata?.Root is null)
        {
            return SettingsResponse.DefaultDateFormatPreference;
        }

        var value = metadata.Root
            .Elements(MetadataRecordElement)
            .Where(element => string.Equals(
                element.Attribute(MetadataRecordIdAttribute)?.Value,
                MetadataDataRecordId,
                StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute(DateFormatPreferenceAttribute)?.Value)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

        return NormalizeDateFormatPreference(value);
    }

    private static string UpsertDateFormatPreference(string? metadataXml, string dateFormatPreference)
    {
        var metadata = ParseMetadata(metadataXml) ?? new XDocument(new XElement(MetadataRoot));
        var root = metadata.Root ?? new XElement(MetadataRoot);

        if (metadata.Root is null)
        {
            metadata.Add(root);
        }

        var dataRecord = root
            .Elements(MetadataRecordElement)
            .FirstOrDefault(element => string.Equals(
                element.Attribute(MetadataRecordIdAttribute)?.Value,
                MetadataDataRecordId,
                StringComparison.OrdinalIgnoreCase));

        if (dataRecord is null)
        {
            dataRecord = new XElement(MetadataRecordElement);
            dataRecord.SetAttributeValue(MetadataRecordIdAttribute, MetadataDataRecordId);
            root.Add(dataRecord);
        }

        dataRecord.SetAttributeValue(DateFormatPreferenceAttribute, dateFormatPreference);
        return metadata.ToString(SaveOptions.DisableFormatting);
    }

    private static XDocument? ParseMetadata(string? metadataXml)
    {
        if (string.IsNullOrWhiteSpace(metadataXml))
        {
            return null;
        }

        try
        {
            return XDocument.Parse(metadataXml);
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeDateFormatPreference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SettingsResponse.DefaultDateFormatPreference;
        }

        var trimmed = value.Trim();
        return SupportedDateFormats.Contains(trimmed)
            ? trimmed
            : SettingsResponse.DefaultDateFormatPreference;
    }

    private sealed record SystemInfoSnapshot(Guid SystemId, string? OwnerName, string? MetadataXml);
}
