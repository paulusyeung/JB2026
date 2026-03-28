using System.Data;
using JB2026.EfCoreSpike;
using JB2026.EfCoreSpike.Data;
using JB2026.EfCoreSpike.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCoreSpike.Tests;

public sealed class EfCoreSpikeValidationTests
{
    [Fact]
    public async Task Complex_entity_crud_roundtrip_succeeds()
    {
        await using var context = CreateContext();

        var orderId = Guid.NewGuid();
        var createdBy = Guid.Parse("f31c57ea-7f08-4a05-b5b5-58b2cdab1001");

        var order = new JobOrder
        {
            OrderId = orderId,
            OrderType = 6,
            OrderNumber = "SPIKE-100",
            JobNumber = 90,
            CustomerName = "EF Core Test Customer",
            CustomerRef = "EFS-900",
            OrderTitle = "EF Core CRUD validation",
            ProductCode = "EF-TST",
            ProductStyle = "Proof",
            OrderedOn = DateTime.UtcNow,
            OrderedBy = "efspike",
            RequiredOn = DateTime.UtcNow.AddDays(3),
            Remarks = "Initial insert",
            Qty = 100m,
            Status = 1,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
            ModifiedOn = DateTime.UtcNow,
            ModifiedBy = createdBy,
            Retired = false,
            JobSchedules =
            [
                new JobSchedule
                {
                    JobScheduleId = Guid.NewGuid(),
                    MachineNumber = "MACHINE-T1",
                    ScheduledOn = DateTime.UtcNow.AddDays(1),
                    Status = 1,
                    Priority = 2
                }
            ],
            JobWorkflows =
            [
                new JobWorkflow
                {
                    JobWorkflowId = Guid.NewGuid(),
                    WorkStatus = 0,
                    WorkIndex = 1,
                    WorkNotes = "Created for EF Core spike"
                }
            ]
        };

        context.JobOrders.Add(order);
        await context.SaveChangesAsync();

        var loaded = await context.JobOrders
            .Include(x => x.JobSchedules)
            .Include(x => x.JobWorkflows)
            .SingleAsync(x => x.OrderId == orderId);

        Assert.Equal("EF Core Test Customer", loaded.CustomerName);
        Assert.Single(loaded.JobSchedules);
        Assert.Single(loaded.JobWorkflows);

        loaded.Remarks = "Updated remark";
        loaded.Status = 2;
        await context.SaveChangesAsync();

        var updated = await context.JobOrders.SingleAsync(x => x.OrderId == orderId);
        Assert.Equal("Updated remark", updated.Remarks);
        Assert.Equal(2, updated.Status);

        var schedules = await context.JobSchedules.Where(x => x.OrderId == orderId).ToListAsync();
        var workflows = await context.JobWorkflows.Where(x => x.OrderId == orderId).ToListAsync();
        context.JobSchedules.RemoveRange(schedules);
        context.JobWorkflows.RemoveRange(workflows);
        context.JobOrders.Remove(updated);
        await context.SaveChangesAsync();

        var deleted = await context.JobOrders.SingleOrDefaultAsync(x => x.OrderId == orderId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Stored_procedure_select_returns_expected_attachment()
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync();

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "spJobAttachment_SelRec";
        command.CommandType = CommandType.StoredProcedure;

        var attachmentIdParameter = command.CreateParameter();
        attachmentIdParameter.ParameterName = "@AttachmentId";
        attachmentIdParameter.Value = Guid.Parse("2f84b2e5-3f73-4d60-9d0d-08dc50c00001");
        command.Parameters.Add(attachmentIdParameter);

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("spring-flyer-proof.pdf", reader["OriginalFileName"]);
    }

    [Fact]
    public async Task Stored_procedure_insert_creates_attachment_row()
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync();

        Guid createdAttachmentId;

        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "spJobAttachment_InsRec";
            command.CommandType = CommandType.StoredProcedure;

            var outputParameter = command.CreateParameter();
            outputParameter.ParameterName = "@AttachmentId";
            outputParameter.DbType = DbType.Guid;
            outputParameter.Direction = ParameterDirection.Output;
            command.Parameters.Add(outputParameter);

            var orderIdParameter = command.CreateParameter();
            orderIdParameter.ParameterName = "@OrderId";
            orderIdParameter.Value = Guid.Parse("1e84b2e5-3f73-4d60-9d0d-08dc50c00002");
            command.Parameters.Add(orderIdParameter);

            var typeParameter = command.CreateParameter();
            typeParameter.ParameterName = "@AttachmentType";
            typeParameter.Value = 1;
            command.Parameters.Add(typeParameter);

            var indexParameter = command.CreateParameter();
            indexParameter.ParameterName = "@AttachmentIndex";
            indexParameter.Value = 99;
            command.Parameters.Add(indexParameter);

            var fileParameter = command.CreateParameter();
            fileParameter.ParameterName = "@OriginalFileName";
            fileParameter.Value = "inserted-by-proc.txt";
            command.Parameters.Add(fileParameter);

            await command.ExecuteNonQueryAsync();
            createdAttachmentId = outputParameter.Value is Guid value
                ? value
                : throw new InvalidOperationException("Stored procedure did not return an attachment id.");
        }

        var attachment = await context.JobAttachments.SingleOrDefaultAsync(x => x.AttachmentId == createdAttachmentId);
        Assert.NotNull(attachment);
        Assert.Equal("inserted-by-proc.txt", attachment!.OriginalFileName);

        context.JobAttachments.Remove(attachment);
        await context.SaveChangesAsync();
    }

    private static Phase2SpikeContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Phase2SpikeContext>()
            .UseSqlServer(Phase2SpikeConnection.ConnectionString)
            .Options;

        return new Phase2SpikeContext(options);
    }
}