using System.Text;
using JB2026.Api.Controllers;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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
        var renderer = new StockProductPdfRenderer(configuration);
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
        var content = Encoding.UTF8.GetString(file.FileContents);

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
        var content = Encoding.UTF8.GetString(file.FileContents);
        var expectedNameHex = ToUtf16Hex("Product Name: 彩盒測試產品");
        var expectedRemarkHex = ToUtf16Hex("Remarks: 備註-中文");
        Assert.Contains(expectedNameHex, content);
        Assert.Contains(expectedRemarkHex, content);
    }

    private static string ToUtf16Hex(string value)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes(value);
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("X2"));
        }

        return builder.ToString();
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
