using JB2026.Api.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Microsoft.Extensions.Options;
using JB2026.Api.Options;

namespace JB2026.Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly IOptions<MailcowOptions> _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<MailcowOptions> options, ILogger<EmailService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailMessageResponse>> GetEmailsAsync(
        string companyDomain,
        IReadOnlyList<string> personEmails,
        string? currentUserEmail,
        string? fallbackEmail,
        string? fallbackPassword,
        CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl))
        {
            _logger.LogWarning("[Email] Mailcow BaseUrl not configured");
            return [];
        }

        var imapHost = ExtractImapHost(cfg.BaseUrl);
        if (string.IsNullOrWhiteSpace(imapHost))
        {
            _logger.LogWarning("[Email] Could not extract IMAP host from {BaseUrl}", cfg.BaseUrl);
            return [];
        }

        var searchTerms = BuildSearchTerms(companyDomain, personEmails);
        if (searchTerms.Count == 0)
        {
            _logger.LogWarning("[Email] No search terms derived for domain={Domain}", companyDomain);
            return [];
        }

        var triedAccounts = new List<string>();

        if (!string.IsNullOrWhiteSpace(currentUserEmail))
        {
            triedAccounts.Add(currentUserEmail);
        }

        var fallback = string.IsNullOrWhiteSpace(fallbackEmail)
            ? cfg.FallbackAccountEmail
            : fallbackEmail;
        var fallbackPwd = string.IsNullOrWhiteSpace(fallbackPassword)
            ? cfg.FallbackAccountPassword
            : fallbackPassword;

        if (!string.IsNullOrWhiteSpace(fallback) && !triedAccounts.Contains(fallback))
        {
            triedAccounts.Add(fallback);
        }

        foreach (var email in triedAccounts)
        {
            var password = email == fallback ? fallbackPwd : fallbackPwd;
            if (string.IsNullOrWhiteSpace(password)) continue;

            try
            {
                var result = await SearchFoldersAsync(imapHost, email, password, searchTerms, cfg, cancellationToken);
                if (result.Count > 0)
                {
                    _logger.LogInformation("[Email] Found {Count} emails for {Email}", result.Count, email);
                    return result;
                }
                _logger.LogInformation("[Email] No results for {Email}, trying next account", email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Email] Failed to search with {Email}", email);
            }
        }

        return [];
    }

    public async Task<EmailDetailResponse?> GetEmailDetailAsync(
        string id,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl))
            return null;

        var imapHost = ExtractImapHost(cfg.BaseUrl);
        if (string.IsNullOrWhiteSpace(imapHost))
            return null;

        var email = cfg.FallbackAccountEmail;
        var password = cfg.FallbackAccountPassword;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        if (!UniqueId.TryParse(id, out var uid))
        {
            _logger.LogWarning("[Email] Invalid UID: {Id}", id);
            return null;
        }

        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(imapHost, cfg.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);

            var imapFolder = await client.GetFolderAsync(folder, cancellationToken);
            await imapFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var message = await imapFolder.GetMessageAsync(uid, cancellationToken);

            var detail = new EmailDetailResponse
            {
                Id = id,
                Folder = folder,
                Sender = message.From?.Mailboxes?.FirstOrDefault()?.Address ?? string.Empty,
                To = message.To?.Mailboxes?.Select(m => m.Address).ToList() ?? [],
                Cc = message.Cc?.Mailboxes?.Select(m => m.Address).ToList() ?? [],
                Subject = message.Subject ?? string.Empty,
                Date = message.Date.Date != DateTimeOffset.MinValue ? message.Date.Date : message.Date.DateTime,
                Size = 0,
                HasAttachment = message.Attachments?.Any() == true,
                BodyText = message.TextBody ?? string.Empty,
                BodyHtml = message.HtmlBody ?? string.Empty,
                Attachments = message.Attachments?.Select(a =>
                {
                    var part = a as MimePart;
                    return new EmailAttachmentInfo
                    {
                        FileName = a.ContentDisposition?.FileName ?? a.ContentType?.Name ?? "attachment",
                        Size = part?.Content?.Stream?.Length ?? 0,
                        MimeType = a.ContentType?.MimeType ?? "application/octet-stream",
                    };
                }).ToList() ?? [],
            };

            await imapFolder.CloseAsync(cancellationToken: cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to fetch detail for UID {Id} in folder {Folder}", id, folder);
            return null;
        }
    }

    public async Task<AttachmentDownloadResult?> DownloadAttachmentAsync(
        string id,
        string folder,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl))
            return null;

        var imapHost = ExtractImapHost(cfg.BaseUrl);
        if (string.IsNullOrWhiteSpace(imapHost))
            return null;

        var email = cfg.FallbackAccountEmail;
        var password = cfg.FallbackAccountPassword;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        if (!UniqueId.TryParse(id, out var uid))
        {
            _logger.LogWarning("[Email] Invalid UID: {Id}", id);
            return null;
        }

        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(imapHost, cfg.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, cancellationToken);
            await client.AuthenticateAsync(email, password, cancellationToken);

            var imapFolder = await client.GetFolderAsync(folder, cancellationToken);
            await imapFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var message = await imapFolder.GetMessageAsync(uid, cancellationToken);

            foreach (var attachment in message.Attachments)
            {
                var attachmentName = attachment.ContentDisposition?.FileName ?? attachment.ContentType?.Name ?? string.Empty;
                if (!string.Equals(attachmentName, fileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (attachment is MimePart part)
                {
                    var memoryStream = new MemoryStream();
                    await part.Content.DecodeToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    return new AttachmentDownloadResult
                    {
                        Content = memoryStream,
                        ContentType = part.ContentType?.MimeType ?? "application/octet-stream",
                        FileName = attachmentName,
                    };
                }

                if (attachment is MessagePart rfc822)
                {
                    var memoryStream = new MemoryStream();
                    await rfc822.Message.WriteToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    return new AttachmentDownloadResult
                    {
                        Content = memoryStream,
                        ContentType = "message/rfc822",
                        FileName = attachmentName,
                    };
                }
            }

            _logger.LogWarning("[Email] Attachment \"{FileName}\" not found for UID {Id} in folder {Folder}", fileName, id, folder);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to download attachment \"{FileName}\" for UID {Id} in folder {Folder}", fileName, id, folder);
            return null;
        }
    }

    private async Task<IReadOnlyList<EmailMessageResponse>> SearchFoldersAsync(
        string imapHost,
        string email,
        string password,
        IReadOnlyList<string> searchTerms,
        MailcowOptions cfg,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Email] Connecting to {Host}:{Port} as {Email}", imapHost, cfg.ImapPort, email);

        using var client = new ImapClient();
        await client.ConnectAsync(imapHost, cfg.ImapPort, MailKit.Security.SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(email, password, cancellationToken);

        _logger.LogInformation("[Email] Authenticated as {Email}, searching folders", email);

        var results = new List<EmailMessageResponse>();
        var seenIds = new HashSet<string>();

        var foldersToSearch = new[] { "INBOX", "Sent", "Sent Messages", "Sent Items" };

        foreach (var folderName in foldersToSearch)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var folder = await client.GetFolderAsync(folderName, cancellationToken);
                await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                SearchQuery query = SearchQuery.FromContains(searchTerms[0]);
                for (var i = 1; i < searchTerms.Count; i++)
                {
                    query = query.Or(SearchQuery.FromContains(searchTerms[i]));
                }
                var uids = await folder.SearchAsync(query, cancellationToken);

                var items = await folder.FetchAsync(uids, MessageSummaryItems.Full | MessageSummaryItems.BodyStructure | MessageSummaryItems.UniqueId, cancellationToken);

                foreach (var item in items)
                {
                    var uid = item.UniqueId.ToString();
                    if (!seenIds.Add(uid)) continue;

                    var sender = item.Envelope?.From?.Mailboxes?.FirstOrDefault()?.Address ?? string.Empty;
                    var subject = item.Envelope?.Subject ?? string.Empty;
                    var date = item.Envelope?.Date ?? DateTimeOffset.MinValue;
                    var size = item.Size ?? 0;
                    var hasAttachment = item.Attachments?.Any() == true;

                    results.Add(new EmailMessageResponse
                    {
                        Id = uid,
                        Sender = sender,
                        Subject = subject,
                        Date = date,
                        Size = size,
                        HasAttachment = hasAttachment,
                        Folder = folderName,
                    });
                }

                await folder.CloseAsync(cancellationToken: cancellationToken);
            }
            catch (FolderNotFoundException)
            {
                _logger.LogDebug("[Email] Folder {Name} not found", folderName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Email] Error searching folder {Name}", folderName);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);

        return results
            .OrderByDescending(e => e.Date)
            .Take(200)
            .ToList()
            .AsReadOnly();
    }

    private static List<string> BuildSearchTerms(string companyDomain, IReadOnlyList<string> personEmails)
    {
        var terms = new List<string>();

        if (!string.IsNullOrWhiteSpace(companyDomain))
        {
            terms.Add($"@{companyDomain.Trim().TrimStart('@')}");
        }

        foreach (var email in personEmails)
        {
            var trimmed = email.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !terms.Contains(trimmed))
            {
                terms.Add(trimmed);
            }
        }

        return terms;
    }

    private static string ExtractImapHost(string baseUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        if (Uri.TryCreate($"https://{baseUrl}", UriKind.Absolute, out uri))
        {
            return uri.Host;
        }

        return baseUrl;
    }
}
