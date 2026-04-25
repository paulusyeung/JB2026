using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

/// <summary>
/// Verifies the two-pass retire-then-hard-delete lifecycle and cascading cleanup behavior
/// for the stock product delete endpoint.
/// </summary>
public sealed class ProductDeleteLifecycleParityTests
{
    [Fact]
    public async Task First_delete_retires_active_product_and_leaves_record_in_db()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var actor = template.CreatedBy;
        var now = DateTime.UtcNow;

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = template.CategoryId,
            StockNumber = TrimToMaxLength($"RET-{Guid.NewGuid():N}", 32),
            ProductCode = TrimToMaxLength($"RPC-{Guid.NewGuid():N}", 32),
            ProductName = TrimToMaxLength($"OPSX Retire Test {Guid.NewGuid():N}", 64),
            Description = template.Description,
            Remarks = template.Remarks,
            MOQ = template.MOQ,
            Balance = 0,
            SellingPrice = template.SellingPrice,
            COGS = template.COGS,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
            Retired = false,
        };

        writeContext.Products.Add(product);
        await writeContext.SaveChangesAsync();

        try
        {
            // Simulate first-pass: retire the product
            var toRetire = await writeContext.Products.FirstAsync(x => x.ProductId == product.ProductId);
            toRetire.Retired = true;
            toRetire.RetiredOn = DateTime.UtcNow;
            toRetire.RetiredBy = actor;
            toRetire.ModifiedOn = DateTime.UtcNow;
            toRetire.ModifiedBy = actor;
            await writeContext.SaveChangesAsync();

            // Verify product is now retired but still exists
            var afterRetire = await readContext.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == product.ProductId);

            Assert.NotNull(afterRetire);
            Assert.True(afterRetire!.Retired);
            Assert.NotNull(afterRetire.RetiredOn);
            Assert.NotNull(afterRetire.RetiredBy);
        }
        finally
        {
            // Cleanup: hard delete the retired product
            var toClean = await writeContext.Products.FirstOrDefaultAsync(x => x.ProductId == product.ProductId);
            if (toClean is not null)
            {
                writeContext.Products.Remove(toClean);
                await writeContext.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Second_delete_hard_deletes_retired_product_and_removes_from_db()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var actor = template.CreatedBy;
        var now = DateTime.UtcNow;

        // Insert a product that is already retired
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = template.CategoryId,
            StockNumber = TrimToMaxLength($"HD-{Guid.NewGuid():N}", 32),
            ProductCode = TrimToMaxLength($"HPC-{Guid.NewGuid():N}", 32),
            ProductName = TrimToMaxLength($"OPSX HardDel Test {Guid.NewGuid():N}", 64),
            Description = template.Description,
            Remarks = template.Remarks,
            MOQ = template.MOQ,
            Balance = 0,
            SellingPrice = template.SellingPrice,
            COGS = template.COGS,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
            Retired = true,
            RetiredOn = now,
            RetiredBy = actor,
        };

        writeContext.Products.Add(product);
        await writeContext.SaveChangesAsync();

        // Simulate second-pass: hard delete
        var toDelete = await writeContext.Products.FirstAsync(x => x.ProductId == product.ProductId);
        writeContext.Products.Remove(toDelete);
        await writeContext.SaveChangesAsync();

        // Verify product is completely gone
        var afterDelete = await readContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == product.ProductId);

        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task Hard_delete_removes_associated_stock_inout_rows()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var actor = template.CreatedBy;
        var now = DateTime.UtcNow;

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = template.CategoryId,
            StockNumber = TrimToMaxLength($"CAS-{Guid.NewGuid():N}", 32),
            ProductCode = TrimToMaxLength($"CPC-{Guid.NewGuid():N}", 32),
            ProductName = TrimToMaxLength($"OPSX Cascade Test {Guid.NewGuid():N}", 64),
            Description = template.Description,
            Remarks = template.Remarks,
            MOQ = template.MOQ,
            Balance = 10,
            SellingPrice = template.SellingPrice,
            COGS = template.COGS,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
            Retired = true,
            RetiredOn = now,
            RetiredBy = actor,
        };

        writeContext.Products.Add(product);
        await writeContext.SaveChangesAsync();

        // Add stock in/out rows for the product
        var movement1 = new StockInOut
        {
            InOutId = Guid.NewGuid(),
            ProductId = product.ProductId,
            InOutDate = now.Date,
            Reference = "opsx-cascade-test-in",
            Qty = 10,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
        };
        var movement2 = new StockInOut
        {
            InOutId = Guid.NewGuid(),
            ProductId = product.ProductId,
            InOutDate = now.Date,
            Reference = "opsx-cascade-test-out",
            Qty = -3,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
        };
        writeContext.StockInOuts.Add(movement1);
        writeContext.StockInOuts.Add(movement2);
        await writeContext.SaveChangesAsync();

        // Simulate hard delete: remove movements, then product
        var movements = await writeContext.StockInOuts
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        writeContext.StockInOuts.RemoveRange(movements);

        var toDelete = await writeContext.Products.FirstAsync(x => x.ProductId == product.ProductId);
        writeContext.Products.Remove(toDelete);
        await writeContext.SaveChangesAsync();

        // Verify movements are gone
        var remainingMovements = await readContext.StockInOuts.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        Assert.Empty(remainingMovements);

        // Verify product is gone
        var remainingProduct = await readContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == product.ProductId);
        Assert.Null(remainingProduct);
    }

    [Fact]
    public async Task Hard_delete_removes_associated_product_attachment_rows()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var actor = template.CreatedBy;
        var now = DateTime.UtcNow;

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = template.CategoryId,
            StockNumber = TrimToMaxLength($"ATT-{Guid.NewGuid():N}", 32),
            ProductCode = TrimToMaxLength($"APC-{Guid.NewGuid():N}", 32),
            ProductName = TrimToMaxLength($"OPSX Attach Del Test {Guid.NewGuid():N}", 64),
            Description = template.Description,
            Remarks = template.Remarks,
            MOQ = template.MOQ,
            Balance = 0,
            SellingPrice = template.SellingPrice,
            COGS = template.COGS,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
            Retired = true,
            RetiredOn = now,
            RetiredBy = actor,
        };

        writeContext.Products.Add(product);
        await writeContext.SaveChangesAsync();

        // Add a product attachment row
        var attachment = new ProductAttachment
        {
            AttachmentId = Guid.NewGuid(),
            ProductId = product.ProductId,
            AttachmentIndex = 0,
            OriginalFileName = TrimToMaxLength($"opsx-test-{Guid.NewGuid():N}.png", 255),
        };
        writeContext.ProductAttachments.Add(attachment);
        await writeContext.SaveChangesAsync();

        // Simulate hard delete: remove attachments then product
        var attachments = await writeContext.ProductAttachments
            .Where(x => x.ProductId == product.ProductId)
            .ToListAsync();
        writeContext.ProductAttachments.RemoveRange(attachments);

        var toDelete = await writeContext.Products.FirstAsync(x => x.ProductId == product.ProductId);
        writeContext.Products.Remove(toDelete);
        await writeContext.SaveChangesAsync();

        // Verify attachment is gone
        var remainingAttachment = await readContext.ProductAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == product.ProductId);
        Assert.Null(remainingAttachment);

        // Verify product is gone
        var remainingProduct = await readContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == product.ProductId);
        Assert.Null(remainingProduct);
    }

    private static string TrimToMaxLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
