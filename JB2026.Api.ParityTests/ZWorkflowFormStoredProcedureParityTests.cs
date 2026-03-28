using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ZWorkflowFormStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_z_workflow_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_WorkflowForms.AsNoTracking().FirstAsync();
        var gateway = new ZWorkflowFormStoredProcedureGateway(readContext, writeContext);

        var workflowFormId = await gateway.InsertAsync(new CreateZWorkflowFormStoredProcedureRequest(
            WorkflowId: template.WorkflowId,
            FormId: template.FormId,
            SeqNumber: template.SeqNumber));

        try
        {
            var procRecord = await gateway.SelectAsync(workflowFormId);
            var tableRecord = await readContext.Z_WorkflowForms.AsNoTracking().FirstOrDefaultAsync(x => x.WorkflowFormId == workflowFormId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.WorkflowFormId, procRecord!.WorkflowFormId);
            Assert.Equal(tableRecord.WorkflowId, procRecord.WorkflowId);
            Assert.Equal(tableRecord.FormId, procRecord.FormId);
            Assert.Equal(tableRecord.SeqNumber, procRecord.SeqNumber);
        }
        finally
        {
            await gateway.DeleteAsync(workflowFormId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_z_workflow_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_WorkflowForms.AsNoTracking().FirstAsync();
        var gateway = new ZWorkflowFormStoredProcedureGateway(readContext, writeContext);

        var workflowFormId = await gateway.InsertAsync(new CreateZWorkflowFormStoredProcedureRequest(
            WorkflowId: template.WorkflowId,
            FormId: template.FormId,
            SeqNumber: template.SeqNumber));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateZWorkflowFormStoredProcedureRequest(
                WorkflowFormId: workflowFormId,
                WorkflowId: template.WorkflowId,
                FormId: template.FormId,
                SeqNumber: template.SeqNumber + 1));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(workflowFormId);
            var tableRecord = await readContext.Z_WorkflowForms.AsNoTracking().FirstOrDefaultAsync(x => x.WorkflowFormId == workflowFormId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SeqNumber, procRecord!.SeqNumber);
            Assert.Equal(tableRecord.WorkflowId, procRecord.WorkflowId);
        }
        finally
        {
            await gateway.DeleteAsync(workflowFormId);
        }
    }
}
