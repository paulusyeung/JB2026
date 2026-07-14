namespace JB2026.Api.Services.TwentyCrm;

public interface ITwentyCrmSyncService
{
    Task<(bool Success, string Message, Guid? UserId)> SyncMemberAsync(
        string email,
        string firstName,
        string lastName);
}
