using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class ReportsControllerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Run_InvalidDays_ReturnsBadRequest(int days)
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.Run(new RunReportRequest
        {
            ReportName = "Exceptional_Report",
            StartOn = new DateOnly(2026, 3, 30),
            Days = days,
            Take = 20,
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void Run_InvalidTake_ReturnsBadRequest(int take)
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.Run(new RunReportRequest
        {
            ReportName = "Exceptional_Report",
            StartOn = new DateOnly(2026, 3, 30),
            Days = 31,
            Take = take,
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void Run_ValidRequest_ReturnsReportResponse()
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.Run(new RunReportRequest
        {
            ReportName = "Exceptional_Report",
            StartOn = new DateOnly(2026, 3, 30),
            Days = 31,
            Take = 1,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ReportRunResponse>(ok.Value);
        Assert.Equal("Exceptional_Report", payload.ReportName);
        Assert.Equal(1, payload.TotalRows);
        Assert.Equal(123.45m, payload.TotalCostA);
        Assert.Single(payload.Rows);
    }

    private static ReportsController CreateController(IQuotationRepository repository)
    {
        var controller = new ReportsController(repository, NullLogger<ReportsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private sealed class StubQuotationRepository : IQuotationRepository
    {
        public IReadOnlyList<QuotationListItemResponse> GetRange(DateOnly startOn, int days)
        {
            return
            [
                new QuotationListItemResponse
                {
                    HeaderId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    MachineType = "1",
                    QuoteNumber = 1001,
                    QuoteNumberIndex = 1,
                    QuoteNumberIndexPair = "1001-1",
                    QuotedOn = startOn.ToDateTime(TimeOnly.MinValue),
                    QuotedBy = "tester",
                    ApprovedOn = null,
                    ApprovedBy = null,
                    PrintTitle = "Exceptional report sample",
                    CustomerName = "Acme",
                    PrintsSize = "A4",
                    PrintsColor = "4C",
                    PrintsQty = 100,
                    MaterialName = "Art Paper",
                    MaterialCost = 20,
                    TotalCostA = 123.45m,
                    UnitCostA = 1.23m,
                    Status = 1,
                },
            ];
        }

        public IReadOnlyList<QuotationListItemResponse> Search(string keyword)
            => [];

        public (byte[] Content, string FileName)? GetPdf(Guid headerId)
            => null;
    }
}