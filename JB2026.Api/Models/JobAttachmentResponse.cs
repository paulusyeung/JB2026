namespace JB2026.Api.Models;

public sealed class JobAttachmentResponse
{
    public required Guid AttachmentId { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long Length { get; init; }

    public int AttachmentType { get; init; }
}

public sealed class JobAttachmentDeleteRequest
{
    public required IReadOnlyList<Guid> AttachmentIds { get; init; }
}
