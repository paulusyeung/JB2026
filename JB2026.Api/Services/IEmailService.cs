using JB2026.Api.Models;

namespace JB2026.Api.Services;

public interface IEmailService
{
    Task<IReadOnlyList<EmailMessageResponse>> GetEmailsAsync(
        string companyDomain,
        IReadOnlyList<string> personEmails,
        string? currentUserEmail,
        string? fallbackEmail,
        string? fallbackPassword,
        CancellationToken cancellationToken = default);

    Task<EmailDetailResponse?> GetEmailDetailAsync(
        string id,
        string folder,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownloadResult?> DownloadAttachmentAsync(
        string id,
        string folder,
        string fileName,
        CancellationToken cancellationToken = default);
}

public sealed class AttachmentDownloadResult : IDisposable
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "attachment";

    public void Dispose() => Content?.Dispose();
}
