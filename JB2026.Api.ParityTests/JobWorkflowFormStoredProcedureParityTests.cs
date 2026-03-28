using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.ParityTests;

public sealed class JobWorkflowFormStoredProcedureParityTests
{
    [Fact]
    public async Task StoredProcedure_insert_and_select_match_table_state_for_job_workflow_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobWorkflowForms.AsNoTracking().FirstAsync();
        var gateway = new JobWorkflowFormStoredProcedureGateway(readContext, writeContext);

        var jobWorkflowFormId = await gateway.InsertAsync(new CreateJobWorkflowFormStoredProcedureRequest(
            JobWorkflowId: template.JobWorkflowId,
            FormId: template.FormId,
            SeqNumber: template.SeqNumber,
            MetadataXml: template.MetadataXml));

        try
        {
            var procRecord = await gateway.SelectAsync(jobWorkflowFormId);
            var tableRecord = await readContext.JobWorkflowForms.AsNoTracking().FirstOrDefaultAsync(x => x.JobWorkflowFormId == jobWorkflowFormId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.JobWorkflowFormId, procRecord!.JobWorkflowFormId);
            Assert.Equal(tableRecord.JobWorkflowId, procRecord.JobWorkflowId);
            Assert.Equal(tableRecord.FormId, procRecord.FormId);
            Assert.Equal(tableRecord.SeqNumber, procRecord.SeqNumber);
        }
        finally
        {
            await gateway.DeleteAsync(jobWorkflowFormId);
        }
    }

    [Fact]
    public async Task StoredProcedure_update_matches_table_state_for_job_workflow_form()
    {
        var connectionString = LegacyConnectionStringHelper.ResolveLegacyProviderConnectionString();
        await using var writeContext = new JB5LegacyWriteContext(
            new DbContextOptionsBuilder<JB5LegacyWriteContext>().UseSqlServer(connectionString).Options);
        await using var readContext = new JB5LegacyReadContext(
            new DbContextOptionsBuilder<JB5LegacyReadContext>().UseSqlServer(connectionString).Options);

        var template = await readContext.JobWorkflowForms.AsNoTracking().FirstAsync();
        var gateway = new JobWorkflowFormStoredProcedureGateway(readContext, writeContext);

        var jobWorkflowFormId = await gateway.InsertAsync(new CreateJobWorkflowFormStoredProcedureRequest(
            JobWorkflowId: template.JobWorkflowId,
            FormId: template.FormId,
            SeqNumber: template.SeqNumber,
            MetadataXml: template.MetadataXml));

        try
        {
            var updated = await gateway.UpdateAsync(new UpdateJobWorkflowFormStoredProcedureRequest(
                JobWorkflowFormId: jobWorkflowFormId,
                JobWorkflowId: template.JobWorkflowId,
                FormId: template.FormId,
                SeqNumber: (template.SeqNumber ?? 0) + 1,
                MetadataXml: template.MetadataXml));

            Assert.True(updated);

            var procRecord = await gateway.SelectAsync(jobWorkflowFormId);
            var tableRecord = await readContext.JobWorkflowForms.AsNoTracking().FirstOrDefaultAsync(x => x.JobWorkflowFormId == jobWorkflowFormId);

            Assert.NotNull(procRecord);
            Assert.NotNull(tableRecord);
            Assert.Equal(tableRecord!.SeqNumber, procRecord!.SeqNumber);
            Assert.Equal(tableRecord.JobWorkflowId, procRecord.JobWorkflowId);
        }
        finally
        {
            await gateway.DeleteAsync(jobWorkflowFormId);
        }
    }
}
