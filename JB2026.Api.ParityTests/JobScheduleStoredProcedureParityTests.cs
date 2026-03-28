using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobScheduleStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_schedule()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobSchedules.AsNoTracking().FirstAsync();
        var gateway = new JobScheduleStoredProcedureGateway(readContext, writeContext);

        var scheduleId = await gateway.InsertAsync(new CreateJobScheduleStoredProcedureRequest(
            OrderId: template.OrderId,
            ScheduledOn: template.ScheduledOn,
            Status: template.Status,
            Priority: template.Priority,
            MachineNumber: $"T{Guid.NewGuid():N}"[..10],
            CompletedOn: template.CompletedOn,
            ShouldReview: template.ShouldReview,
            UrgencyLevel: template.UrgencyLevel,
            Cancelled: template.Cancelled,
            CancelledOn: template.CancelledOn,
            CancelledBy: template.CancelledBy,
            RescheduledCount: template.RescheduledCount,
            RescheduledBy: template.RescheduledBy,
            RescheduledOn: template.RescheduledOn));

        try
        {
            var procRecord = await gateway.SelectAsync(scheduleId);
            var tableRecord = await readContext.JobSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.ScheduleId, procRecord!.ScheduleId);
            Assert.Equal(tableRecord.OrderId, procRecord.OrderId);
            Assert.Equal(tableRecord.MachineNumber, procRecord.MachineNumber);
            Assert.Equal(tableRecord.UrgencyLevel, procRecord.UrgencyLevel);
        }
        finally
        {
            await gateway.DeleteAsync(scheduleId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_schedule()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobSchedules.AsNoTracking().FirstAsync();
        var gateway = new JobScheduleStoredProcedureGateway(readContext, writeContext);

        var scheduleId = await gateway.InsertAsync(new CreateJobScheduleStoredProcedureRequest(
            OrderId: template.OrderId,
            ScheduledOn: template.ScheduledOn,
            Status: template.Status,
            Priority: template.Priority,
            MachineNumber: template.MachineNumber,
            CompletedOn: template.CompletedOn,
            ShouldReview: template.ShouldReview,
            UrgencyLevel: template.UrgencyLevel,
            Cancelled: template.Cancelled,
            CancelledOn: template.CancelledOn,
            CancelledBy: template.CancelledBy,
            RescheduledCount: template.RescheduledCount,
            RescheduledBy: template.RescheduledBy,
            RescheduledOn: template.RescheduledOn));

        try
        {
            var updatedMachine = $"U{Guid.NewGuid():N}"[..10];
            var updated = await gateway.UpdateAsync(new UpdateJobScheduleStoredProcedureRequest(
                ScheduleId: scheduleId,
                OrderId: template.OrderId,
                ScheduledOn: template.ScheduledOn,
                Status: 9,
                Priority: 9,
                MachineNumber: updatedMachine,
                CompletedOn: template.CompletedOn,
                ShouldReview: template.ShouldReview,
                UrgencyLevel: template.UrgencyLevel,
                Cancelled: template.Cancelled,
                CancelledOn: template.CancelledOn,
                CancelledBy: template.CancelledBy,
                RescheduledCount: template.RescheduledCount,
                RescheduledBy: template.RescheduledBy,
                RescheduledOn: template.RescheduledOn));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(scheduleId);
            var tableRecord = await readContext.JobSchedules.AsNoTracking().FirstOrDefaultAsync(x => x.ScheduleId == scheduleId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.Status, procRecord!.Status);
            Assert.Equal(tableRecord.Priority, procRecord.Priority);
            Assert.Equal(tableRecord.MachineNumber, procRecord.MachineNumber);
        }
        finally
        {
            await gateway.DeleteAsync(scheduleId);
        }
    }
}
