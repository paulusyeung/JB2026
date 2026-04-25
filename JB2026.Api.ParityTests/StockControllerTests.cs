using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var composer = new StockProductPrintComposer(context);
        var renderer = new StockProductPdfRenderer(configuration);
        var controller = new StockController(context, NullLogger<StockController>.Instance, configuration, composer, renderer);
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

    [Fact]
    public async Task ProductRecord_CreateUpdateDelete_WorksEndToEnd()
    {
        using var context = CreateContext(nameof(ProductRecord_CreateUpdateDelete_WorksEndToEnd));
        var categoryId = Guid.NewGuid();
        context.Z_Categories.Add(new Z_Category
        {
            CategoryId = categoryId,
            CategoryCode = "CAT",
            CategoryName = "Category",
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var createResult = await controller.CreateProductRecord(new StockProductRecordUpsertRequest
        {
            CustomerCode = "CUS",
            CategoryCode = "CAT",
            SequenceNumber = "1",
            ProductCode = "P-100",
            ProductName = "Demo Product",
            ProductionInfo = "Info",
            Remarks = "Remarks",
            SellingPrice = 12.5m,
            COGS = 10.0m,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdBody = Assert.IsType<StockProductRecordResponse>(created.Value);
        Assert.Equal("CUS-CAT-0001", createdBody.StockNumber);

        var updateResult = await controller.UpdateProductRecord(createdBody.ProductId, new StockProductRecordUpsertRequest
        {
            CustomerCode = "CUS",
            CategoryCode = "CAT",
            SequenceNumber = "2",
            ProductCode = "P-100-UPDATED",
            ProductName = "Updated Product",
            ProductionInfo = "Updated Info",
            Remarks = "Updated Remarks",
            SellingPrice = 14.5m,
            COGS = 11.0m,
        }, CancellationToken.None);

        var updated = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updatedBody = Assert.IsType<StockProductRecordResponse>(updated.Value);
        Assert.Equal("P-100-UPDATED", updatedBody.ProductCode);
        Assert.Equal("CUS-CAT-0002", updatedBody.StockNumber);

        var deleteResult = await controller.DeleteProductRecord(createdBody.ProductId, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);

        var getAfterDelete = await controller.GetProductRecord(createdBody.ProductId, CancellationToken.None);
        Assert.IsType<NotFoundResult>(getAfterDelete.Result);
    }

    [Fact]
    public async Task ValidateProductCodeUniqueness_ExcludesCurrentProduct_WhenProvided()
    {
        using var context = CreateContext(nameof(ValidateProductCodeUniqueness_ExcludesCurrentProduct_WhenProvided));
        var targetId = Guid.NewGuid();
        context.Products.Add(new Product
        {
            ProductId = targetId,
            ProductCode = "P-EXIST",
            ProductName = "Existing",
            StockNumber = "CUS-CAT-0001",
            Balance = 10,
            SellingPrice = 9,
            COGS = 7,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = false,
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var selfResult = await controller.ValidateProductCodeUniqueness("P-EXIST", targetId, CancellationToken.None);
        var selfOk = Assert.IsType<OkObjectResult>(selfResult.Result);
        var selfBody = Assert.IsType<StockProductCodeValidationResponse>(selfOk.Value);
        Assert.True(selfBody.IsUnique);

        var otherResult = await controller.ValidateProductCodeUniqueness("P-EXIST", null, CancellationToken.None);
        var otherOk = Assert.IsType<OkObjectResult>(otherResult.Result);
        var otherBody = Assert.IsType<StockProductCodeValidationResponse>(otherOk.Value);
        Assert.False(otherBody.IsUnique);
    }

    [Fact]
    public async Task GetProductMovements_ReturnsRunningBalance()
    {
        using var context = CreateContext(nameof(GetProductMovements_ReturnsRunningBalance));
        var productId = Guid.NewGuid();

        context.StockInOuts.AddRange(
            new StockInOut
            {
                InOutId = Guid.NewGuid(),
                ProductId = productId,
                InOutDate = new DateTime(2026, 1, 1),
                Reference = "IN-1",
                Qty = 10,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
            },
            new StockInOut
            {
                InOutId = Guid.NewGuid(),
                ProductId = productId,
                InOutDate = new DateTime(2026, 1, 2),
                Reference = "OUT-1",
                Qty = -4,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetProductMovements(productId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsType<List<StockMovementHistoryItemResponse>>(ok.Value);
        Assert.Equal(2, items.Count);
        Assert.Equal(10, items[0].RunningBalance);
        Assert.Equal(6, items[1].RunningBalance);
    }

    [Fact]
    public async Task GetNextProductNumber_ReturnsIncrementedSequence()
    {
        using var context = CreateContext(nameof(GetNextProductNumber_ReturnsIncrementedSequence));

        context.Products.AddRange(
            new Product
            {
                ProductId = Guid.NewGuid(),
                ProductCode = "P-1",
                ProductName = "Alpha",
                StockNumber = "CUS-CAT-0003",
                Balance = 1,
                SellingPrice = 1,
                COGS = 1,
                MOQ = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
            },
            new Product
            {
                ProductId = Guid.NewGuid(),
                ProductCode = "P-2",
                ProductName = "Beta",
                StockNumber = "CUS-CAT-0009",
                Balance = 1,
                SellingPrice = 1,
                COGS = 1,
                MOQ = 1,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid(),
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = Guid.NewGuid(),
                Retired = false,
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetNextProductNumber("CUS", "CAT", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<StockProductNextNumberResponse>(ok.Value);
        Assert.Equal("0010", body.SequenceNumber);
        Assert.Equal("CUS-CAT-0010", body.StockNumber);
    }

    // Task 6.3: Integration/parity tests for transaction persistence and balance recalculation

    private static Product CreateTestProduct(JB5LegacyReadContext context, int initialBalance = 100)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "TEST-001",
            ProductName = "Test Product",
            StockNumber = "TST-CAT-0001",
            Balance = initialBalance,
            SellingPrice = 10,
            COGS = 7,
            MOQ = 1,
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

    [Fact]
    public async Task CreateStockInOutTransaction_PositiveQty_AddsToBalance()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_PositiveQty_AddsToBalance));
        var product = CreateTestProduct(context, initialBalance: 100);
        var controller = CreateController(context);

        var result = await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Reference = "IN-001",
            Qty = 50,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<StockInOutTransactionResult>(created.Value);
        Assert.Equal(150, body.NewBalance);
        Assert.Equal(product.ProductId, body.ProductId);

        var updatedProduct = await context.Products.FindAsync(product.ProductId);
        Assert.NotNull(updatedProduct);
        Assert.Equal(150, updatedProduct!.Balance);

        var movement = await context.StockInOuts.FirstOrDefaultAsync(m => m.ProductId == product.ProductId);
        Assert.NotNull(movement);
        Assert.Equal(50, movement!.Qty);
        Assert.Equal("IN-001", movement.Reference);
    }

    [Fact]
    public async Task CreateStockInOutTransaction_NegativeQty_SubtractsFromBalance()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_NegativeQty_SubtractsFromBalance));
        var product = CreateTestProduct(context, initialBalance: 100);
        var controller = CreateController(context);

        var result = await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = -30,
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<StockInOutTransactionResult>(created.Value);
        Assert.Equal(70, body.NewBalance);

        var updatedProduct = await context.Products.FindAsync(product.ProductId);
        Assert.Equal(70, updatedProduct!.Balance);
    }

    [Fact]
    public async Task CreateStockInOutTransaction_ZeroQty_ReturnsBadRequest()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_ZeroQty_ReturnsBadRequest));
        var product = CreateTestProduct(context);
        var controller = CreateController(context);

        var result = await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = 0,
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateStockInOutTransaction_UnknownProductId_ReturnsNotFound()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_UnknownProductId_ReturnsNotFound));
        var controller = CreateController(context);

        var result = await controller.CreateStockInOutTransaction(Guid.NewGuid(), new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = 10,
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateStockInOutTransaction_RetiredProduct_ReturnsNotFound()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_RetiredProduct_ReturnsNotFound));
        var retiredProduct = new Product
        {
            ProductId = Guid.NewGuid(),
            ProductCode = "RETIRED-001",
            ProductName = "Retired Product",
            StockNumber = "RET-CAT-0001",
            Balance = 10,
            SellingPrice = 1,
            COGS = 1,
            MOQ = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = Guid.NewGuid(),
            Retired = true,
        };
        context.Products.Add(retiredProduct);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.CreateStockInOutTransaction(retiredProduct.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = 5,
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateStockInOutTransaction_MultipleTransactions_BalanceCumulatesCorrectly()
    {
        using var context = CreateContext(nameof(CreateStockInOutTransaction_MultipleTransactions_BalanceCumulatesCorrectly));
        var product = CreateTestProduct(context, initialBalance: 0);
        var controller = CreateController(context);

        await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = 100,
        }, CancellationToken.None);

        await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = -40,
        }, CancellationToken.None);

        await controller.CreateStockInOutTransaction(product.ProductId, new StockInOutTransactionRequest
        {
            InOutDate = DateTime.Today,
            Qty = 20,
        }, CancellationToken.None);

        var updatedProduct = await context.Products.FindAsync(product.ProductId);
        Assert.Equal(80, updatedProduct!.Balance);

        var movementCount = await context.StockInOuts.CountAsync(m => m.ProductId == product.ProductId);
        Assert.Equal(3, movementCount);
    }
}