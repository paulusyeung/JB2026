namespace JB2026.ApiPilot.Models;

public sealed class JobAttachmentDto
{
    public required string AttachmentType { get; init; }

    public required string FileName { get; init; }
}