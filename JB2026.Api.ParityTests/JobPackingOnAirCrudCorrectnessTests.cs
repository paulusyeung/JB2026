using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobPackingOnAirCrudCorrectnessTests
{
    [Fact]
    public async Task EfCore_crud_roundtrip_persists_expected_values_for_job_packing_on_air()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobPackingOnAirs.AsNoTracking().FirstAsync();

        var created = new JobPackingOnAir
        {
            OnAirId = Guid.NewGuid(),
            OrderId = template.OrderId,
            OnAiredOn = template.OnAiredOn,
            OnAiredBy = template.OnAiredBy,
            Priority = (template.Priority ?? 0) + 1,
            Status = (template.Status ?? 0) + 1,
            CompletedOn = template.CompletedOn,
            CompletedBy = template.CompletedBy,
            Cancelled = template.Cancelled,
            CancelledOn = template.CancelledOn,
            CancelledBy = template.CancelledBy,
            RescheduledCount = (template.RescheduledCount ?? 0) + 1,
            RescheduledOn = template.RescheduledOn,
            RescheduledBy = template.RescheduledBy
        };

        writeContext.JobPackingOnAirs.Add(created);
        await writeContext.SaveChangesAsync();

        try
        {
            var inserted = await readContext.JobPackingOnAirs.AsNoTracking().FirstOrDefaultAsync(x => x.OnAirId == created.OnAirId);
            Assert.NotNull(inserted);
            Assert.Equal(created.OrderId, inserted!.OrderId);
            Assert.Equal(created.Priority, inserted.Priority);
            Assert.Equal(created.Status, inserted.Status);

            created.Priority = (created.Priority ?? 0) + 2;
            created.Status = (created.Status ?? 0) + 2;
            created.Cancelled = !(created.Cancelled ?? false);
            created.RescheduledCount = (created.RescheduledCount ?? 0) + 2;

            writeContext.JobPackingOnAirs.Update(created);
            await writeContext.SaveChangesAsync();

            var updated = await readContext.JobPackingOnAirs.AsNoTracking().FirstOrDefaultAsync(x => x.OnAirId == created.OnAirId);
            Assert.NotNull(updated);
            Assert.Equal(created.Priority, updated!.Priority);
            Assert.Equal(created.Status, updated.Status);
            Assert.Equal(created.Cancelled, updated.Cancelled);
            Assert.Equal(created.RescheduledCount, updated.RescheduledCount);
        }
        finally
        {
            var toDelete = await writeContext.JobPackingOnAirs.FirstOrDefaultAsync(x => x.OnAirId == created.OnAirId);
            if (toDelete is not null)
            {
                writeContext.JobPackingOnAirs.Remove(toDelete);
                await writeContext.SaveChangesAsync();
            }
        }

        var deleted = await readContext.JobPackingOnAirs.AsNoTracking().FirstOrDefaultAsync(x => x.OnAirId == created.OnAirId);
        Assert.Null(deleted);
    }
}
