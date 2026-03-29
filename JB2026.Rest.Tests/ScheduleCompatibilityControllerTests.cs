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

        var historyTopic = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => x.Topic)
                .FirstOrDefaultAsync());

        Assert.Equal("OnReadyPaper", historyTopic);
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

        var historyTopic = await _factory.ReadAsync(ctx =>
            ctx.FCMHistories
                .OrderByDescending(x => x.DeliveredOn)
                .Select(x => x.Topic)
                .FirstOrDefaultAsync());

        Assert.Equal("OnReadyPlate", historyTopic);
    }
}
