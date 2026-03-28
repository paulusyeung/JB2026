using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ProductCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_product()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.Products.AsNoTracking().FirstAsync();
        var timestamp = DateTime.UtcNow;

        var created = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = template.CategoryId,
            StockNumber = TrimToMaxLength($"STK-{Guid.NewGuid():N}", 32),
            ProductCode = TrimToMaxLength($"PC-{Guid.NewGuid():N}", 32),
            ProductName = TrimToMaxLength($"PN-{Guid.NewGuid():N}", 128),
            Description = template.Description,
            Remarks = template.Remarks,
            MOQ = template.MOQ,
            Balance = template.Balance,
            SellingPrice = template.SellingPrice,
            COGS = template.COGS,
            CreatedOn = timestamp,
            CreatedBy = template.CreatedBy,
            ModifiedOn = timestamp,
            ModifiedBy = template.ModifiedBy,
            Retired = false
        };

        writeContext.Products.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == created.ProductId);
            Assert.NotNull(inserted);
            Assert.Equal(created.ProductCode, inserted!.ProductCode);
            Assert.Equal(created.ProductName, inserted.ProductName);
            Assert.Equal(created.SellingPrice, inserted.SellingPrice);
            Assert.False(inserted.Retired);

            created.ProductName = TrimToMaxLength($"PN-UPD-{Guid.NewGuid():N}", 128);
            created.SellingPrice = created.SellingPrice + 1.5m;
            created.Balance = created.Balance + 10;
            created.ModifiedOn = timestamp.AddMinutes(1);
            writeContext.Products.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.Products.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == created.ProductId);
            Assert.NotNull(updated);
            Assert.Equal(created.ProductName, updated!.ProductName);
            Assert.Equal(created.SellingPrice, updated.SellingPrice);
            Assert.Equal(created.Balance, updated.Balance);
        }
        finally
        {
            var toDelete = await writeContext.Products.FirstOrDefaultAsync(x => x.ProductId == created.ProductId);
            if (toDelete is not null)
            {
                writeContext.Products.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == created.ProductId);
        Assert.Null(deleted);
    }

    private static string TrimToMaxLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
