using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ZWorkflowStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_z_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_Workflows.AsNoTracking().FirstAsync();
        var gateway = new ZWorkflowStoredProcedureGateway(readContext, writeContext);

        var workflowId = await gateway.InsertAsync(new CreateZWorkflowStoredProcedureRequest(
            WorkflowName: TrimToMaxLength($"OPSX-WF-{Guid.NewGuid():N}", 64),
            WorkTitle: TrimToMaxLength($"OPSX-WFT-{Guid.NewGuid():N}", 512),
            WorkInstruction: TrimToMaxLength($"OPSX-WFI-{Guid.NewGuid():N}", 512)));

        try
        {
            var procRecord = await gateway.SelectAsync(workflowId);
            var tableRecord = await readContext.Z_Workflows.AsNoTracking().FirstOrDefaultAsync(x => x.WorkflowId == workflowId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.WorkflowId, procRecord!.WorkflowId);
            Assert.Equal(tableRecord.WorkflowName, procRecord.WorkflowName);
            Assert.Equal(tableRecord.WorkTitle, procRecord.WorkTitle);
            Assert.Equal(tableRecord.WorkInstruction, procRecord.WorkInstruction);
        }
        finally
        {
            await gateway.DeleteAsync(workflowId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_z_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var gateway = new ZWorkflowStoredProcedureGateway(readContext, writeContext);

        var workflowId = await gateway.InsertAsync(new CreateZWorkflowStoredProcedureRequest(
            WorkflowName: TrimToMaxLength($"OPSX-WF-U-{Guid.NewGuid():N}", 64),
            WorkTitle: TrimToMaxLength($"OPSX-WFT-U-{Guid.NewGuid():N}", 512),
            WorkInstruction: TrimToMaxLength($"OPSX-WFI-U-{Guid.NewGuid():N}", 512)));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateZWorkflowStoredProcedureRequest(
                WorkflowId: workflowId,
                WorkflowName: TrimToMaxLength($"OPSX-WF-V-{Guid.NewGuid():N}", 64),
                WorkTitle: TrimToMaxLength($"OPSX-WFT-V-{Guid.NewGuid():N}", 512),
                WorkInstruction: TrimToMaxLength($"OPSX-WFI-V-{Guid.NewGuid():N}", 512)));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(workflowId);
            var tableRecord = await readContext.Z_Workflows.AsNoTracking().FirstOrDefaultAsync(x => x.WorkflowId == workflowId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.WorkflowName, procRecord!.WorkflowName);
            Assert.Equal(tableRecord.WorkTitle, procRecord.WorkTitle);
        }
        finally
        {
            await gateway.DeleteAsync(workflowId);
        }
    }

    private static string TrimToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
