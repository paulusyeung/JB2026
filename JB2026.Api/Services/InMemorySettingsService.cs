using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemorySettingsService : ISettingsService
{
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

    private readonly object _sync = new();
    private SettingsResponse _settings = new()
    {
        CompanyName = "JB2026 Printing",
        TimeZone = "Asia/Kuala_Lumpur",
        CurrencyCode = "MYR",
        EnableLegacyFallback = true,
        OwnerName = "Marche Label & Printing Limited",
        NextOrderNumber = "168360",
        NextProductNumber = "005356",
        NextQuotationNumber = "170024",
        CommonQueryIndex = 2,
        CompletedQueryIndex = 1,
        ScheduleQueryRange = 1,
        GmailAccount = "job.book@marchehk.com",
        GmailPassword = "24110810",
        DateFormatPreference = SettingsResponse.DefaultDateFormatPreference,
        JobListDaysBack = 90,
    };

    public SettingsResponse Get()
    {
        lock (_sync)
        {
            return _settings;
        }
    }

    public SettingsResponse Update(UpdateSettingsRequest request)
    {
        lock (_sync)
        {
            _settings = new SettingsResponse
            {
                CompanyName = request.CompanyName.Trim(),
                TimeZone = request.TimeZone.Trim(),
                CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
                EnableLegacyFallback = request.EnableLegacyFallback,
                OwnerName = request.OwnerName.Trim(),
                NextOrderNumber = request.NextOrderNumber.Trim(),
                NextProductNumber = request.NextProductNumber.Trim(),
                NextQuotationNumber = request.NextQuotationNumber.Trim(),
                CommonQueryIndex = request.CommonQueryIndex,
                CompletedQueryIndex = request.CompletedQueryIndex,
                ScheduleQueryRange = request.ScheduleQueryRange,
                GmailAccount = request.GmailAccount.Trim(),
                GmailPassword = request.GmailPassword.Trim(),
                DateFormatPreference = NormalizeDateFormatPreference(request.DateFormatPreference),
                JobListDaysBack = request.JobListDaysBack,
            };

            return _settings;
        }
    }

    public Task<string> AllocateNextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var allocated = _settings.NextOrderNumber;
            _settings = WithNextOrderNumber(_settings, IncrementOrderNumber(allocated));

            return Task.FromResult(allocated);
        }
    }

    /// <summary>
    /// Mirrors the legacy JB5 allocator (Job.Book/Controls/Utility.cs GetNextOrderNumber):
    /// hand out the current value, then persist its successor padded to six digits.
    /// </summary>
    internal static string IncrementOrderNumber(string current)
    {
        var next = int.TryParse(current, out var parsed) ? parsed + 1 : 1;

        return next.ToString().PadLeft(6, '0');
    }

    private static SettingsResponse WithNextOrderNumber(SettingsResponse source, string nextOrderNumber)
    {
        return new SettingsResponse
        {
            CompanyName = source.CompanyName,
            TimeZone = source.TimeZone,
            CurrencyCode = source.CurrencyCode,
            EnableLegacyFallback = source.EnableLegacyFallback,
            OwnerName = source.OwnerName,
            NextOrderNumber = nextOrderNumber,
            NextProductNumber = source.NextProductNumber,
            NextQuotationNumber = source.NextQuotationNumber,
            CommonQueryIndex = source.CommonQueryIndex,
            CompletedQueryIndex = source.CompletedQueryIndex,
            ScheduleQueryRange = source.ScheduleQueryRange,
            GmailAccount = source.GmailAccount,
            GmailPassword = source.GmailPassword,
            DateFormatPreference = source.DateFormatPreference,
            JobListDaysBack = source.JobListDaysBack,
        };
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
}