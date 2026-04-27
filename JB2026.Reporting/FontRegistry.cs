using System.Reflection;
using System.Threading;
using QuestPDF.Drawing;

namespace JB2026.Reporting;

public static class FontRegistry
{
    public const string LatinFontFamily = "Lato";
    public const string CjkFontFamily = "Noto Sans CJK SC";

    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        RegisterEmbeddedFont("JB2026.Reporting.Fonts.Lato-Regular.ttf");
        RegisterEmbeddedFont("JB2026.Reporting.Fonts.NotoSansCJKsc-Regular.otf");
    }

    private static void RegisterEmbeddedFont(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource not found: {resourceName}");

        FontManager.RegisterFont(stream);
    }
}
