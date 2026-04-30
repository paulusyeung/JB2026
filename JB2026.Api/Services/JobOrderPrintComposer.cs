using JB2026.Api.Models;
using JB2026.Api.Options;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JB2026.Api.Services;

public sealed class JobOrderPrintComposer : IJobOrderPrintComposer
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    private readonly JB5LegacyReadContext _readContext;
    private readonly LegacyFilesOptions _legacyFiles;

    public JobOrderPrintComposer(JB5LegacyReadContext readContext, IOptions<LegacyFilesOptions> legacyFiles)
    {
        _readContext = readContext;
        _legacyFiles = legacyFiles.Value;
    }

    public async Task<JobOrderPrintDocument?> ComposeAsync(Guid orderId, JobOrderPrintRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _readContext.JobOrders
            .AsNoTracking()
            .Include(o => o.JobWorkflows)
            .Include(o => o.JobAttachments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && !o.Retired, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var compositeOrderNumber = BuildCompositeOrderNumber(order.OrderNumber, order.JobNumber);
        var baseOrderNumber = ExtractBaseOrderNumber(order.OrderNumber);

        var allWorkflows = order.JobWorkflows
            .OrderBy(w => w.WorkIndex)
            .ToList();

        var selectedWorkflows = request.SelectedWorkflowIndices.Count > 0
            ? allWorkflows
                .Where(w => request.SelectedWorkflowIndices.Contains(w.WorkIndex))
                .ToList()
            : allWorkflows;

        byte[]? imageBytes = null;
        if (!request.NoPicture)
        {
            imageBytes = LoadFirstImageBytes(orderId, baseOrderNumber, order.OrderedOn, order.JobAttachments);
        }

        return new JobOrderPrintDocument
        {
            OrderNumber = compositeOrderNumber,
            CustomerName = order.CustomerName,
            CustomerRef = order.CustomerRef,
            OrderTitle = order.OrderTitle,
            ProductCode = order.ProductCode,
            ProductStyle = order.ProductStyle,
            ProductDetails = order.ProductDetails,
            OrderedBy = order.OrderedBy,
            PaymentTerms = order.PaymentTerms,
            Remarks = order.Remarks,
            OrderedOn = order.OrderedOn,
            ModifiedOn = order.ModifiedOn,
            RequiredOn = order.RequiredOn,
            InvoiceRef = order.InvoiceRef,
            InvoiceAmount = order.InvoiceAmount,
            Qty = order.Qty,
            NoPicture = request.NoPicture,
            NoProductDetails = request.NoProductDetails,
            ImageBytes = imageBytes,
            Workflows = selectedWorkflows.Select(w => new JobOrderPrintWorkflow
            {
                WorkIndex = w.WorkIndex,
                WorkTitle = w.WorkTitle,
                WorkInstruction = w.WorkInstruction,
                WorkNotes = w.WorkNotes
            }).ToList()
        };
    }

    private byte[]? LoadFirstImageBytes(Guid orderId, string baseOrderNumber, DateTime? orderedOn, IEnumerable<EfCore.Models.JobAttachment> attachments)
    {
        var imageAttachments = attachments
            .OrderBy(a => a.AttachmentIndex)
            .Where(a => !string.IsNullOrWhiteSpace(a.OriginalFileName)
                        && ImageExtensions.Contains(Path.GetExtension(a.OriginalFileName)))
            .ToList();

        foreach (var attachment in imageAttachments)
        {
            var filePath = LocateAttachmentFile(orderId, baseOrderNumber, orderedOn, attachment.OriginalFileName!, attachment.AttachmentType.ToString());
            if (filePath is not null && File.Exists(filePath))
            {
                try
                {
                    return File.ReadAllBytes(filePath);
                }
                catch (IOException)
                {
                    // File unreadable; try next attachment
                }
            }
        }

        return null;
    }

    private string? LocateAttachmentFile(Guid orderId, string orderNumber, DateTime? orderedOn, string fileName, string? attachmentType)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return null;
        }

        var probes = new List<string>();
        var typeFolder = string.IsNullOrWhiteSpace(attachmentType) ? string.Empty
            : attachmentType.All(char.IsDigit) ? attachmentType : string.Empty;

        foreach (var root in GetLegacyRoots())
        {
            var legacyOrder = Path.Combine(root, orderNumber);
            probes.Add(legacyOrder);
            if (!string.IsNullOrWhiteSpace(typeFolder))
            {
                probes.Add(Path.Combine(legacyOrder, typeFolder));
            }

            if (orderedOn.HasValue)
            {
                var migratedOrder = Path.Combine(root, "JB5", orderedOn.Value.ToString("yyyy"), orderedOn.Value.ToString("MM"), orderNumber);
                probes.Add(migratedOrder);
                if (!string.IsNullOrWhiteSpace(typeFolder))
                {
                    probes.Add(Path.Combine(migratedOrder, typeFolder));
                }
            }
        }

        foreach (var cloudRoot in ExpandRootCandidates(_legacyFiles.CloudDiskRoot))
        {
            probes.Add(Path.Combine(cloudRoot, "uploads", orderId.ToString("N")));
        }

        foreach (var folder in probes.Distinct())
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            var directPath = Path.Combine(folder, safeFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            try
            {
                var recursive = Directory
                    .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .FirstOrDefault(p => string.Equals(Path.GetFileName(p), safeFileName, StringComparison.OrdinalIgnoreCase));
                if (recursive is not null)
                {
                    return recursive;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip unreadable folders
            }
        }

        return null;
    }

    private IReadOnlyList<string> GetLegacyRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in ExpandRootCandidates(_legacyFiles.FileAgentRoot))
        {
            roots.Add(root);
        }
        foreach (var root in ExpandRootCandidates(_legacyFiles.InBox))
        {
            roots.Add(root);
        }
        return roots.ToList();
    }

    private static IReadOnlyList<string> ExpandRootCandidates(string? configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Array.Empty<string>();
        }

        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { configuredRoot };

        if (configuredRoot.StartsWith("\\\\", StringComparison.Ordinal))
        {
            var parts = configuredRoot.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var tail = parts.Skip(2).ToArray();
                roots.Add('/' + string.Join('/', parts));
                if (tail.Length > 0)
                {
                    roots.Add('/' + string.Join('/', tail));
                }
            }
        }

        return roots.ToList();
    }

    private static string BuildCompositeOrderNumber(string? orderNumber, int? jobNumber)
    {
        var lhs = orderNumber ?? string.Empty;
        return jobNumber.HasValue ? $"{lhs}-{jobNumber.Value:00}" : lhs;
    }

    private static string ExtractBaseOrderNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var lastDash = trimmed.LastIndexOf('-');
        if (lastDash > 0)
        {
            var suffix = trimmed[(lastDash + 1)..];
            if (suffix.All(char.IsDigit))
            {
                return trimmed[..lastDash];
            }
        }

        return trimmed;
    }
}
