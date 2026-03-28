using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobWorkflowStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobWorkflows.AsNoTracking().FirstAsync();
        var gateway = new JobWorkflowStoredProcedureGateway(readContext, writeContext);

        var jobWorkflowId = await gateway.InsertAsync(new CreateJobWorkflowStoredProcedureRequest(
            OrderId: template.OrderId,
            WorkflowId: template.WorkflowId,
            WorkIndex: template.WorkIndex,
            WorkTitle: TrimToMaxLength($"OPSX-JWF-{Guid.NewGuid():N}", 64),
            WorkInstruction: TrimToMaxLength($"OPSX-JWF-I-{Guid.NewGuid():N}", 128),
            WorkStatus: template.WorkStatus,
            WorkNotes: $"OPSX-PARITY-{Guid.NewGuid():N}",
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy));

        try
        {
            var procRecord = await gateway.SelectAsync(jobWorkflowId);
            var tableRecord = await readContext.JobWorkflows.AsNoTracking().FirstOrDefaultAsync(x => x.JobWorkflowId == jobWorkflowId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.JobWorkflowId, procRecord!.JobWorkflowId);
            Assert.Equal(tableRecord.OrderId, procRecord.OrderId);
            Assert.Equal(tableRecord.WorkflowId, procRecord.WorkflowId);
            Assert.Equal(tableRecord.WorkIndex, procRecord.WorkIndex);
            Assert.Equal(tableRecord.WorkTitle, procRecord.WorkTitle);
        }
        finally
        {
            await gateway.DeleteAsync(jobWorkflowId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobWorkflows.AsNoTracking().FirstAsync();
        var gateway = new JobWorkflowStoredProcedureGateway(readContext, writeContext);

        var jobWorkflowId = await gateway.InsertAsync(new CreateJobWorkflowStoredProcedureRequest(
            OrderId: template.OrderId,
            WorkflowId: template.WorkflowId,
            WorkIndex: template.WorkIndex,
            WorkTitle: TrimToMaxLength($"OPSX-JWF-U-{Guid.NewGuid():N}", 64),
            WorkInstruction: TrimToMaxLength($"OPSX-JWF-UI-{Guid.NewGuid():N}", 128),
            WorkStatus: template.WorkStatus,
            WorkNotes: $"OPSX-PARITY-U-{Guid.NewGuid():N}",
            ModifiedOn: template.ModifiedOn,
            ModifiedBy: template.ModifiedBy));

        try
        {
            var updatedTitle = TrimToMaxLength($"OPSX-JWF-V-{Guid.NewGuid():N}", 64);
            var updated = await gateway.UpdateAsync(new UpdateJobWorkflowStoredProcedureRequest(
                JobWorkflowId: jobWorkflowId,
                OrderId: template.OrderId,
                WorkflowId: template.WorkflowId,
                WorkIndex: template.WorkIndex + 1,
                WorkTitle: updatedTitle,
                WorkInstruction: TrimToMaxLength($"OPSX-JWF-VI-{Guid.NewGuid():N}", 128),
                WorkStatus: template.WorkStatus,
                WorkNotes: $"OPSX-PARITY-V-{Guid.NewGuid():N}",
                ModifiedOn: template.ModifiedOn,
                ModifiedBy: template.ModifiedBy));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(jobWorkflowId);
            var tableRecord = await readContext.JobWorkflows.AsNoTracking().FirstOrDefaultAsync(x => x.JobWorkflowId == jobWorkflowId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.WorkIndex, procRecord!.WorkIndex);
            Assert.Equal(tableRecord.WorkTitle, procRecord.WorkTitle);
        }
        finally
        {
            await gateway.DeleteAsync(jobWorkflowId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
