namespace JB2026.Api.Services.TwentyCrm;

public interface ITwentyCrmService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
