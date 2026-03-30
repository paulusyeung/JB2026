using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class SmlControllerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void GetStats_InvalidDays_ReturnsBadRequest(int days)
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.GetStats(new DateOnly(2026, 3, 1), days, 100);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetStats_InvalidTake_ReturnsBadRequest(int take)
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.GetStats(new DateOnly(2026, 3, 1), 31, take);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void GetStats_ValidRequest_ReturnsAggregates()
    {
        var controller = CreateController(new StubQuotationRepository());

        var result = controller.GetStats(new DateOnly(2026, 3, 1), 31, 100);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<SmlStatsResponse>(ok.Value);
        Assert.Equal(3, payload.RowCount);
        Assert.Equal(600m, payload.TotalAmount);
        Assert.Equal(2, payload.Monthly.Count);
        Assert.Equal("Acme", payload.TopCustomers[0].CustomerName);
        Assert.Equal(300m, payload.TopCustomers[0].Amount);
    }

    private static SmlController CreateController(IQuotationRepository repository)
    {
        var controller = new SmlController(repository, NullLogger<SmlController>.Instance);
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
                    QuotedOn = new DateTime(2026, 3, 10),
                    QuotedBy = "alice",
                    ApprovedOn = null,
                    ApprovedBy = null,
                    PrintTitle = "Rtf A",
                    CustomerName = "Acme",
                    PrintsSize = "A4",
                    PrintsColor = "4C",
                    PrintsQty = 100,
                    MaterialName = "Paper",
                    MaterialCost = 10,
                    TotalCostA = 100m,
                    UnitCostA = 1m,
                    Status = 1,
                },
                new QuotationListItemResponse
                {
                    HeaderId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    MachineType = "1",
                    QuoteNumber = 1002,
                    QuoteNumberIndex = 1,
                    QuoteNumberIndexPair = "1002-1",
                    QuotedOn = new DateTime(2026, 3, 15),
                    QuotedBy = "bob",
                    ApprovedOn = null,
                    ApprovedBy = null,
                    PrintTitle = "Rtf B",
                    CustomerName = "Acme",
                    PrintsSize = "A4",
                    PrintsColor = "4C",
                    PrintsQty = 120,
                    MaterialName = "Paper",
                    MaterialCost = 12,
                    TotalCostA = 200m,
                    UnitCostA = 2m,
                    Status = 1,
                },
                new QuotationListItemResponse
                {
                    HeaderId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    MachineType = "1",
                    QuoteNumber = 1003,
                    QuoteNumberIndex = 1,
                    QuoteNumberIndexPair = "1003-1",
                    QuotedOn = new DateTime(2026, 4, 5),
                    QuotedBy = "cara",
                    ApprovedOn = null,
                    ApprovedBy = null,
                    PrintTitle = "Rtf C",
                    CustomerName = "Beta",
                    PrintsSize = "A4",
                    PrintsColor = "4C",
                    PrintsQty = 140,
                    MaterialName = "Paper",
                    MaterialCost = 14,
                    TotalCostA = 300m,
                    UnitCostA = 3m,
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