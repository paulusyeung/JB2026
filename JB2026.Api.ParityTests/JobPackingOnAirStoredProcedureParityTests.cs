using JB2026.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobPackingOnAirStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_packing_on_air()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobPackingOnAirs.AsNoTracking().FirstAsync();
        var gateway = new JobPackingOnAirStoredProcedureGateway(readContext, writeContext);

        var onAirId = await gateway.InsertAsync(new CreateJobPackingOnAirStoredProcedureRequest(
            OrderId: template.OrderId,
            OnAiredOn: template.OnAiredOn,
            OnAiredBy: template.OnAiredBy,
            Priority: template.Priority,
            Status: template.Status,
            CompletedOn: template.CompletedOn,
            CompletedBy: template.CompletedBy,
            Cancelled: template.Cancelled,
            CancelledOn: template.CancelledOn,
            CancelledBy: template.CancelledBy,
            RescheduledCount: template.RescheduledCount,
            RescheduledOn: template.RescheduledOn,
            RescheduledBy: template.RescheduledBy));

        try
        {
            var procRecord = await gateway.SelectAsync(onAirId);
            var tableRecord = await readContext.JobPackingOnAirs.AsNoTracking().FirstOrDefaultAsync(x => x.OnAirId == onAirId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OnAirId, procRecord!.OnAirId);
            Assert.Equal(tableRecord.OrderId, procRecord.OrderId);
            Assert.Equal(tableRecord.Priority, procRecord.Priority);
            Assert.Equal(tableRecord.Status, procRecord.Status);
            Assert.Equal(tableRecord.Cancelled, procRecord.Cancelled);
        }
        finally
        {
            await gateway.DeleteAsync(onAirId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_packing_on_air()
    {
        await using var writeContext = LegacyDbContextFactory.CreateWriteContext();
        await using var readContext = LegacyDbContextFactory.CreateReadContext();

        var template = await readContext.JobPackingOnAirs.AsNoTracking().FirstAsync();
        var gateway = new JobPackingOnAirStoredProcedureGateway(readContext, writeContext);

        var onAirId = await gateway.InsertAsync(new CreateJobPackingOnAirStoredProcedureRequest(
            OrderId: template.OrderId,
            OnAiredOn: template.OnAiredOn,
            OnAiredBy: template.OnAiredBy,
            Priority: template.Priority,
            Status: template.Status,
            CompletedOn: template.CompletedOn,
            CompletedBy: template.CompletedBy,
            Cancelled: template.Cancelled,
            CancelledOn: template.CancelledOn,
            CancelledBy: template.CancelledBy,
            RescheduledCount: template.RescheduledCount,
            RescheduledOn: template.RescheduledOn,
            RescheduledBy: template.RescheduledBy));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateJobPackingOnAirStoredProcedureRequest(
                OnAirId: onAirId,
                OrderId: template.OrderId,
                OnAiredOn: template.OnAiredOn.AddDays(1),
                OnAiredBy: template.OnAiredBy,
                Priority: (template.Priority ?? 0) + 1,
                Status: (template.Status ?? 0) + 1,
                CompletedOn: template.CompletedOn.AddDays(1),
                CompletedBy: template.CompletedBy,
                Cancelled: !(template.Cancelled ?? false),
                CancelledOn: template.CancelledOn.AddDays(1),
                CancelledBy: template.CancelledBy,
                RescheduledCount: (template.RescheduledCount ?? 0) + 1,
                RescheduledOn: template.RescheduledOn.AddDays(1),
                RescheduledBy: template.RescheduledBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(onAirId);
            var tableRecord = await readContext.JobPackingOnAirs.AsNoTracking().FirstOrDefaultAsync(x => x.OnAirId == onAirId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OnAiredOn, procRecord!.OnAiredOn);
            Assert.Equal(tableRecord.Priority, procRecord.Priority);
            Assert.Equal(tableRecord.Status, procRecord.Status);
            Assert.Equal(tableRecord.Cancelled, procRecord.Cancelled);
            Assert.Equal(tableRecord.RescheduledCount, procRecord.RescheduledCount);
        }
        finally
        {
            await gateway.DeleteAsync(onAirId);
        }
    }
}
