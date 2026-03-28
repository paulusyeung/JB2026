using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class ZOrderTypeWorkflowStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_z_order_type_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_OrderTypeWorkflows.AsNoTracking().FirstAsync();
        var gateway = new ZOrderTypeWorkflowStoredProcedureGateway(readContext, writeContext);

        var id = await gateway.InsertAsync(new CreateZOrderTypeWorkflowStoredProcedureRequest(
            WorkflowId: template.WorkflowId,
            OrderType: template.OrderType,
            WorkIndex: template.WorkIndex));

        try
        {
            var procRecord = await gateway.SelectAsync(id);
            var tableRecord = await readContext.Z_OrderTypeWorkflows.AsNoTracking().FirstOrDefaultAsync(x => x.OrderTypeWorkflowId == id);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.OrderTypeWorkflowId, procRecord!.OrderTypeWorkflowId);
            Assert.Equal(tableRecord.WorkflowId, procRecord.WorkflowId);
            Assert.Equal(tableRecord.OrderType, procRecord.OrderType);
            Assert.Equal(tableRecord.WorkIndex, procRecord.WorkIndex);
        }
        finally
        {
            await gateway.DeleteAsync(id);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_z_order_type_workflow()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.Z_OrderTypeWorkflows.AsNoTracking().FirstAsync();
        var gateway = new ZOrderTypeWorkflowStoredProcedureGateway(readContext, writeContext);

        var id = await gateway.InsertAsync(new CreateZOrderTypeWorkflowStoredProcedureRequest(
            WorkflowId: template.WorkflowId,
            OrderType: template.OrderType,
            WorkIndex: template.WorkIndex));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateZOrderTypeWorkflowStoredProcedureRequest(
                OrderTypeWorkflowId: id,
                WorkflowId: template.WorkflowId,
                OrderType: template.OrderType,
                WorkIndex: template.WorkIndex + 1));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(id);
            var tableRecord = await readContext.Z_OrderTypeWorkflows.AsNoTracking().FirstOrDefaultAsync(x => x.OrderTypeWorkflowId == id);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.WorkIndex, procRecord!.WorkIndex);
            Assert.Equal(tableRecord.OrderType, procRecord.OrderType);
        }
        finally
        {
            await gateway.DeleteAsync(id);
        }
    }
}
