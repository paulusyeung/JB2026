using JB2026.Api.Models;
using QuestPDF.Fluent;

namespace JB2026.Api.Services;

public sealed class JobOrderPdfRenderer : IJobOrderPdfRenderer
{
    public byte[] Render(JobOrderPrintDocument document)
    {
        var report = new JobOrderQuestDocument(document);
        return report.GeneratePdf();
    }
}
