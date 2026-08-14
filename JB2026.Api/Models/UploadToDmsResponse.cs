namespace JB2026.Api.Models;

public sealed class UploadToDmsResponse
{
    public required bool AlreadyExists { get; init; }

    public int? DocumentId { get; init; }

    public required string Title { get; init; }
}