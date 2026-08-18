using System.Runtime.CompilerServices;
using JB2026.Reporting;
using QuestPDF.Infrastructure;

namespace JB2026.Api.ParityTests;

internal static class QuestPdfTestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        FontRegistry.EnsureInitialized();
    }
}