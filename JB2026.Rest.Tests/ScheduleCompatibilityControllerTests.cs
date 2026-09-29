using System.Net;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Tests;

public sealed class ScheduleCompatibilityControllerTests : IClassFixture<RestTestFixture>
{
    private readonly RestTestFixture _factory;

    public ScheduleCompatibilityControllerTests(RestTestFixture factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostRegister_WorkflowNotFound_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/Schedule/{Guid.NewGuid()}/0/2", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_PaperReady_PersistsOnReadyPaperHistory()
    {
        var orderId = Guid.NewGuid();
        await _factory.SeedAsync(ctx =>
        {
            ctx.JobWorkflows.Add(new JobWorkflow
            {
                JobWorkflowId = Guid.NewGuid(),
                OrderId = orderId,
                WorkIndex = 0,
                WorkStatus = 1,
                ModifiedOn = DateTime.Now
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/Schedule/{orderId}/0/2", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => new { x.Topic, x.MessageTitle })
                .FirstOrDefaultAsync());

        Assert.NotNull(history);
        Assert.Equal("Device", history.Topic);
        Assert.Equal("JB5 有紙", history.MessageTitle);
    }

    [Fact]
    public async Task PostRegister_PlateReady_PersistsOnReadyPlateHistory()
    {
        var orderId = Guid.NewGuid();
        await _factory.SeedAsync(ctx =>
        {
            ctx.JobWorkflows.Add(new JobWorkflow
            {
                JobWorkflowId = Guid.NewGuid(),
                OrderId = orderId,
                WorkIndex = 1,
                WorkStatus = 1,
                ModifiedOn = DateTime.Now
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/Schedule/{orderId}/1/2", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => new { x.Topic, x.MessageTitle })
                .FirstOrDefaultAsync());

        Assert.NotNull(history);
        Assert.Equal("Device", history.Topic);
        Assert.Equal("JB5 有鋅", history.MessageTitle);
    }

    [Fact]
    public async Task PostRegister_AlreadyReady_PersistsNoSecondHistoryRow()
    {
        // Re-posting the same ready status is not a transition into ready, so it must
        // not record another row (spec: "Only one record per event occurrence").
        var orderId = Guid.NewGuid();
        await _factory.SeedAsync(ctx =>
        {
            ctx.JobWorkflows.Add(new JobWorkflow
            {
                JobWorkflowId = Guid.NewGuid(),
                OrderId = orderId,
                WorkIndex = 0,
                WorkStatus = 2,
                ModifiedOn = DateTime.Now
            });
        });

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/Schedule/{orderId}/0/2", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // No JobOrder is seeded, so the publisher composes the body from the order id.
        // Scoped to this order because the class fixture shares one database.
        var count = await _factory.ReadAsync(ctx => ctx.FCMHistories
            .CountAsync(x => x.MessageBody == orderId.ToString()));
        Assert.Equal(0, count);
    }
}
