using JB2026.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace JB2026.Api.Services;

public sealed class JobOrderPdfRenderer : IJobOrderPdfRenderer
{
    public byte[] Render(JobOrderPrintDocument document)
    {
        IDocument report = string.Equals(document.Layout, "purchaseOrder", StringComparison.OrdinalIgnoreCase)
            ? new PurchaseOrderQuestDocument(document)
            : new JobOrderQuestDocument(document);
        return report.GeneratePdf();
    }
}
