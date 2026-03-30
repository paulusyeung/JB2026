using JB2026.Api.Models;

namespace JB2026.Api.Services;

public sealed class InMemorySettingsService : ISettingsService
{
    private readonly object _sync = new();
    private SettingsResponse _settings = new()
    {
        CompanyName = "JB2026 Printing",
        TimeZone = "Asia/Kuala_Lumpur",
        CurrencyCode = "MYR",
        EnableLegacyFallback = true,
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
            };

            return _settings;
        }
    }
}