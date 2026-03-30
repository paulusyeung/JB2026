using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface ISettingsService
{
    SettingsResponse Get();

    SettingsResponse Update(UpdateSettingsRequest request);
}