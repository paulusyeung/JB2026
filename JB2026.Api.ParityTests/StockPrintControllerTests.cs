using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.Reporting;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using UglyToad.PdfPig;

namespace JB2026.Api.ParityTests;

public sealed class StockPrintControllerTests
{
    private static JB5LegacyReadContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<JB5LegacyReadContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new JB5LegacyReadContext(options);
    }

    private static StockController CreateController(JB5LegacyReadContext context)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var composer = new StockProductPrintComposer(context);
        var renderer = new StockProductPdfRenderer();
        var controller = new StockController(context, NullLogger<StockController>.Instance, configuration, composer, renderer)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task PrintProductRecord_ReturnsPdfFile_ForExistingProduct()
    {
        using var context = CreateContext(nameof(PrintProductRecord_ReturnsPdfFile_ForExistingProduct));
        var product = SeedProduct(context, "P-PRINT", "Print Test", "CUS-CAT-0001");

        var controller = CreateController(context);
        var result = await controller.PrintProductRecord(product.ProductId, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Contains("stock-record-CUS-CAT-0001.pdf", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task PrintProductRecord_ReturnsNotFound_WhenProductMissing()
    {
        using var context = CreateContext(nameof(PrintProductRecord_ReturnsNotFound_WhenProductMissing));
        var controller = CreateController(context);

        var result = await controller.PrintProductRecord(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PrintProductRecord_IncludesRequiredSections_AndDeterministicOrdering()
    {
        using var context = CreateContext(nameof(PrintProductRecord_IncludesRequiredSections_AndDeterministicOrdering));
        var product = SeedProduct(context, "P-ORDER", "Order Test", "CUS-CAT-0002");

        var day = new DateTime(2026, 4, 20);
        context.StockInOuts.AddRange(
            new StockInOut
            {
                InOutId = Guid.NewGuid(),
                ProductId = product.ProductId,
                InOutDate = day,
                Reference = "REF-OLD",
                Qty = 10,
                CreatedOn = day,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = day.AddHours(8),
                ModifiedBy = Guid.NewGuid(),
            },
            new StockInOut
            {
                InOutId = Guid.NewGuid(),
                ProductId = product.ProductId,
                InOutDate = day,
                Reference = "REF-NEW",
                Qty = -2,
                CreatedOn = day,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = day.AddHours(11),
                ModifiedBy = Guid.NewGuid(),
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.PrintProductRecord(product.ProductId, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("Stock Number:", content);
        Assert.Contains("Product Code:", content);
        Assert.Contains("Product Name:", content);
        Assert.Contains("MOQ:", content);
        Assert.Contains("Balance:", content);
        Assert.Contains("REF-NEW", content);
        Assert.Contains("REF-OLD", content);

        var firstIndex = content.IndexOf("REF-OLD", StringComparison.Ordinal);
        var secondIndex = content.IndexOf("REF-NEW", StringComparison.Ordinal);
        Assert.True(firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex);
    }

    [Fact]
    public async Task PrintProductRecord_PreservesMultilingualTextBytes()
    {
        using var context = CreateContext(nameof(PrintProductRecord_PreservesMultilingualTextBytes));
        var product = SeedProduct(context, "P-CJK", "彩盒測試產品", "CUS-CAT-0003", remarks: "備註-中文");

        var controller = CreateController(context);
        var result = await controller.PrintProductRecord(product.ProductId, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        var content = ExtractText(file.FileContents);

        Assert.Contains("彩盒測試產品", content);
        Assert.Contains("備註-中文", content);
    }

    [Fact]
    public void StockPrintDocument_GetMetadata_ReturnsExpectedTitleAndAuthor()
    {
        var model = new StockProductPrintDocument
        {
            ProductId = Guid.NewGuid(),
            StockNumber = "CUS-CAT-0001",
            ProductCode = "P-001",
            ProductName = "產品A",
            ProductionInfo = "Prod",
            Remarks = "Remark",
            MOQ = 10,
            Balance = 50,
            Movements = []
        };

        var document = new StockPrintDocument(model);
        var metadata = document.GetMetadata();

        Assert.Equal("Stock Record Movement Report", metadata.Title);
        Assert.Equal("JB2026.Api", metadata.Author);
    }

    [Fact]
    public void StockPrintDocument_AppliesDeterministicOrderingAndRenumbering()
    {
        var date = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc);
        var model = new StockProductPrintDocument
        {
            ProductId = Guid.NewGuid(),
            StockNumber = "CUS-CAT-0002",
            ProductCode = "P-002",
            ProductName = "Order test",
            ProductionInfo = "Prod",
            Remarks = "Remark",
            MOQ = 10,
            Balance = 50,
            Movements =
            [
                new StockProductPrintMovementRow
                {
                    RowNumber = 99,
                    InOutDate = date,
                    Reference = "REF-NEW",
                    Qty = -3,
                    RunningBalance = 7,
                    ModifiedOn = date.AddHours(3),
                    ModifiedBy = "User2"
                },
                new StockProductPrintMovementRow
                {
                    RowNumber = 10,
                    InOutDate = date,
                    Reference = "REF-OLD",
                    Qty = 10,
                    RunningBalance = 10,
                    ModifiedOn = date.AddHours(1),
                    ModifiedBy = "User1"
                }
            ]
        };

        var document = new StockPrintDocument(model);
        var text = ExtractText(document.GeneratePdf());

        var oldIndex = text.IndexOf("REF-OLD", StringComparison.Ordinal);
        var newIndex = text.IndexOf("REF-NEW", StringComparison.Ordinal);
        Assert.True(oldIndex >= 0 && newIndex >= 0 && oldIndex < newIndex);
    }

    [Fact]
    public void StockPrintDocument_RendersCjkFixtures()
    {
        var model = new StockProductPrintDocument
        {
            ProductId = Guid.NewGuid(),
            StockNumber = "CUS-CAT-0003",
            ProductCode = "P-003",
            ProductName = "中文產品",
            ProductionInfo = "Production",
            Remarks = "備註資料",
            MOQ = 12,
            Balance = 34,
            Movements =
            [
                new StockProductPrintMovementRow
                {
                    RowNumber = 1,
                    InOutDate = new DateTime(2026, 4, 20),
                    Reference = "入庫-樣本",
                    Qty = 5,
                    RunningBalance = 5,
                    ModifiedOn = new DateTime(2026, 4, 20, 9, 0, 0),
                    ModifiedBy = "測試員"
                }
            ]
        };

        var document = new StockPrintDocument(model);
        var text = ExtractText(document.GeneratePdf());

        Assert.Contains("中文產品", text);
        Assert.Contains("備註資料", text);
        Assert.Contains("入庫-樣本", text);
    }

    [Fact]
    public void FontRegistry_Initialization_IsIdempotent()
    {
        FontRegistry.EnsureInitialized();
        FontRegistry.EnsureInitialized();

        var model = new StockProductPrintDocument
        {
            ProductId = Guid.NewGuid(),
            StockNumber = "CUS-CAT-0004",
            ProductCode = "P-004",
            ProductName = "Font Init",
            ProductionInfo = "Prod",
            Remarks = "Remark",
            MOQ = 1,
            Balance = 2,
            Movements = []
        };

        var bytes = new StockPrintDocument(model).GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    private static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join("\n", document.GetPages().Select(page => page.Text));
    }

    private static Product SeedProduct(JB5LegacyReadContext context, string code, string name, string stockNumber, string? remarks = null)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = code,
            ProductName = name,
            StockNumber = stockNumber,
            Description = "Production text",
            Remarks = remarks ?? "General remarks",
            MOQ = 5,
            Balance = 88,
            SellingPrice = 100,
            COGS = 50,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        };

        context.Products.Add(product);
        context.SaveChanges();
        return product;
    }
}
