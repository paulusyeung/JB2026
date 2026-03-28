using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobOrderCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_job_order()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobOrders.AsNoTracking().FirstAsync();
        var timestamp = DateTime.UtcNow;

        var created = new JobOrder
        {
            OrderId = Guid.NewGuid(),
            OrderType = template.OrderType,
            OrderNumber = TrimToMaxLength($"ORD-{Guid.NewGuid():N}", 10),
            JobNumber = (template.JobNumber ?? 0) + 1,
            CustomerName = TrimToMaxLength($"CUST-{Guid.NewGuid():N}", 128),
            CustomerRef = TrimToMaxLength($"REF-{Guid.NewGuid():N}", 32),
            OrderTitle = TrimToMaxLength($"TITLE-{Guid.NewGuid():N}", 128),
            ProductCode = TrimToMaxLength($"PC-{Guid.NewGuid():N}", 32),
            ProductStyle = template.ProductStyle,
            ProductDetails = template.ProductDetails,
            OrderedOn = template.OrderedOn,
            OrderedBy = template.OrderedBy,
            OutputRef = template.OutputRef,
            InvoiceRef = template.InvoiceRef,
            InvoiceAmount = template.InvoiceAmount,
            Qty = template.Qty,
            QtyText = template.QtyText,
            RequiredOn = template.RequiredOn,
            CompletedOn = template.CompletedOn,
            SONumber = template.SONumber,
            PONumber = template.PONumber,
            OriginalSONumber = template.OriginalSONumber,
            OriginalPONumber = template.OriginalPONumber,
            PaymentTerms = template.PaymentTerms,
            Remarks = template.Remarks,
            Status = template.Status,
            CreatedOn = timestamp,
            CreatedBy = template.CreatedBy,
            ModifiedOn = timestamp,
            ModifiedBy = template.ModifiedBy,
            Retired = false
        };

        writeContext.JobOrders.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.JobOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == created.OrderId);
            Assert.NotNull(inserted);
            Assert.Equal(created.OrderNumber, inserted!.OrderNumber);
            Assert.Equal(created.OrderType, inserted.OrderType);
            Assert.Equal(created.CustomerName, inserted.CustomerName);
            Assert.Equal(created.Status, inserted.Status);
            Assert.False(inserted.Retired);

            created.OrderTitle = TrimToMaxLength($"TITLE-UPD-{Guid.NewGuid():N}", 128);
            created.Status = template.Status + 1;
            created.ModifiedOn = timestamp.AddMinutes(1);
            writeContext.JobOrders.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.JobOrders.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == created.OrderId);
            Assert.NotNull(updated);
            Assert.Equal(created.OrderTitle, updated!.OrderTitle);
            Assert.Equal(created.Status, updated.Status);
        }
        finally
        {
            var toDelete = await writeContext.JobOrders.FirstOrDefaultAsync(x => x.OrderId == created.OrderId);
            if (toDelete is not null)
            {
                writeContext.JobOrders.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.JobOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == created.OrderId);
        Assert.Null(deleted);
    }

    private static string TrimToMaxLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
