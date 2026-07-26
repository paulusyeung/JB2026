namespace JB2026.Api.Models;

public sealed class EmailMessageResponse
{
    public string Id { get; set; } = string.Empty;

    public string Sender { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset Date { get; set; }

    public long Size { get; set; }

    public bool HasAttachment { get; set; }

    public string Folder { get; set; } = string.Empty;
}

public sealed class EmailDetailResponse
{
    public string Id { get; set; } = string.Empty;

    public string Sender { get; set; } = string.Empty;

    public IReadOnlyList<string> To { get; set; } = [];

    public IReadOnlyList<string> Cc { get; set; } = [];

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset Date { get; set; }

    public long Size { get; set; }

    public bool HasAttachment { get; set; }

    public string BodyText { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string Folder { get; set; } = string.Empty;

    public IReadOnlyList<EmailAttachmentInfo> Attachments { get; set; } = [];
}

public sealed class EmailAttachmentInfo
{
    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }

    public string MimeType { get; set; } = string.Empty;
}
