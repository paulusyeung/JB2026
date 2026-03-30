using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class StockControllerTests
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
        var controller = new StockController(context, NullLogger<StockController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task GetProducts_InvalidTake_ReturnsBadRequest(int take)
    {
        using var context = CreateContext(nameof(GetProducts_InvalidTake_ReturnsBadRequest) + take);
        var controller = CreateController(context);

        var result = await controller.GetProducts(null, take, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ExcludesRetiredProducts()
    {
        using var context = CreateContext(nameof(GetProducts_ExcludesRetiredProducts));

        context.Products.Add(new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P-A",
            ProductName = "Alpha",
            StockNumber = "STK-A",
            Balance = 12,
            SellingPrice = 10,
            COGS = 7,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false
        });

        context.Products.Add(new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P-B",
            ProductName = "Beta",
            StockNumber = "STK-B",
            Balance = 5,
            SellingPrice = 12,
            COGS = 8,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = true
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetProducts(null, 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<StockProductListItemResponse>>(ok.Value);
        var item = Assert.Single(items);
        Assert.Equal("P-A", item.ProductCode);
    }

    [Fact]
    public async Task GetProducts_FiltersByKeyword()
    {
        using var context = CreateContext(nameof(GetProducts_FiltersByKeyword));

        context.Products.Add(new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P-ABC",
            ProductName = "Alpha",
            StockNumber = "STK-001",
            Balance = 12,
            SellingPrice = 10,
            COGS = 7,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false
        });

        context.Products.Add(new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "P-XYZ",
            ProductName = "Omega",
            StockNumber = "STK-999",
            Balance = 2,
            SellingPrice = 30,
            COGS = 22,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetProducts("ABC", 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<StockProductListItemResponse>>(ok.Value);
        var item = Assert.Single(items);
        Assert.Equal("P-ABC", item.ProductCode);
    }
}