using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace JB2026.Reporting;

public abstract class DocumentBase<TModel> : IDocument
{
    protected DocumentBase(TModel model)
    {
        Model = model;
    }

    protected TModel Model { get; }

    protected static TextStyle LatinTextStyle => TextStyle.Default.FontFamily(FontRegistry.LatinFontFamily).FontSize(10);

    protected static TextStyle CjkTextStyle => TextStyle.Default.FontFamily(FontRegistry.CjkFontFamily).FontSize(10);

    protected static TextStyle SelectTextStyle(bool useCjkFallback)
    {
        return useCjkFallback ? CjkTextStyle : LatinTextStyle;
    }

    public abstract DocumentMetadata GetMetadata();

    public abstract void Compose(IDocumentContainer container);
}
