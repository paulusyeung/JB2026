namespace JB2026.Api.Models;

public sealed class JobAttachmentResponse
{
    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }
}
