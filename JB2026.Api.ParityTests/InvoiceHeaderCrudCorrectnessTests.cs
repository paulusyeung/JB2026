using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class InvoiceHeaderCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_invoice_header()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.InvoiceHeaders.AsNoTracking().FirstAsync();
        var timestamp = DateTime.UtcNow;

        var created = new InvoiceHeader
        {
            HeaderId = Guid.NewGuid(),
            CustomerId = template.CustomerId,
            BillTo = TrimToMaxLength($"BILLTO-{Guid.NewGuid():N}", 256),
            ShipTo = TrimToMaxLength($"SHIPTO-{Guid.NewGuid():N}", 256),
            InvoiceDate = template.InvoiceDate,
            InvoiceNumber = TrimToMaxLength($"INV-{Guid.NewGuid():N}", 8),
            InvoiceAmount = template.InvoiceAmount,
            ICNumber = template.ICNumber,
            CreatedOn = timestamp,
            CreatedBy = template.CreatedBy,
            ModifiedOn = timestamp,
            ModifiedBy = template.ModifiedBy,
            Retired = false
        };

        writeContext.InvoiceHeaders.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.InvoiceHeaders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeaderId == created.HeaderId);
            Assert.NotNull(inserted);
            Assert.Equal(created.InvoiceNumber, inserted!.InvoiceNumber);
            Assert.Equal(created.InvoiceAmount, inserted.InvoiceAmount);
            Assert.Equal(created.BillTo, inserted.BillTo);
            Assert.False(inserted.Retired);

            created.InvoiceNumber = TrimToMaxLength($"INV-UPD-{Guid.NewGuid():N}", 8);
            created.InvoiceAmount = (created.InvoiceAmount ?? 0) + 100m;
            created.BillTo = TrimToMaxLength($"BILLTO-UPD-{Guid.NewGuid():N}", 256);
            created.ModifiedOn = timestamp.AddMinutes(1);
            writeContext.InvoiceHeaders.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.InvoiceHeaders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeaderId == created.HeaderId);
            Assert.NotNull(updated);
            Assert.Equal(created.InvoiceNumber, updated!.InvoiceNumber);
            Assert.Equal(created.InvoiceAmount, updated.InvoiceAmount);
            Assert.Equal(created.BillTo, updated.BillTo);
        }
        finally
        {
            var toDelete = await writeContext.InvoiceHeaders.FirstOrDefaultAsync(x => x.HeaderId == created.HeaderId);
            if (toDelete is not null)
            {
                writeContext.InvoiceHeaders.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.InvoiceHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HeaderId == created.HeaderId);
        Assert.Null(deleted);
    }

    private static string TrimToMaxLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
