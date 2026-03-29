using JB2026.Api.Services;
using JB2026.Rest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class QuotationCompatibilityController : ControllerBase
{
    private readonly IQuotationRepository _repository;

    public QuotationCompatibilityController(IQuotationRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("api/Qt/{starton:datetime}/{days:int}")]
    public IActionResult GetQt(DateTime starton, int days)
    {
        if (days is <= 0 or > 366)
        {
            return BadRequest("days must be between 1 and 366");
        }

        var result = _repository
            .GetRange(DateOnly.FromDateTime(starton), days)
            .Select(ToCompatibilityListItem)
            .ToList();
        return Ok(result);
    }

    [HttpGet("api/Qt/Keyword/{keyword}")]
    public IActionResult GetQtByKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 3)
        {
            return Ok(Array.Empty<object>());
        }

        var result = _repository
            .Search(keyword)
            .Select(ToCompatibilityListItem)
            .ToList();
        return Ok(result);
    }

    [HttpGet("api/Qt/pdf/{id:guid}")]
    public IActionResult GetQtPdf(Guid id)
    {
        var pdf = _repository.GetPdf(id);
        if (pdf is null)
        {
            return NotFound();
        }

        return File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
    }

    private static QuotationCompatibilityListItem ToCompatibilityListItem(JB2026.Api.Models.QuotationListItemResponse source)
    {
        return new QuotationCompatibilityListItem
        {
            HeaderId = source.HeaderId,
            MachineType = source.MachineType,
            QuoteNumber = source.QuoteNumber,
            QuoteNumberIndex = source.QuoteNumberIndex,
            QuoteNumberIndexPair = source.QuoteNumberIndexPair,
            QuotedOn = source.QuotedOn,
            QuotedBy = source.QuotedBy,
            ApprovedOn = source.ApprovedOn,
            ApprovedBy = source.ApprovedBy,
            PrintTitle = source.PrintTitle,
            CustomerId = 0,
            CustomerName = source.CustomerName,
            PrintsSize = source.PrintsSize,
            PrintsColor = source.PrintsColor,
            PrintsQty = source.PrintsQty,
            PaperSheetSize = source.PrintsSize,
            MaterialName = source.MaterialName,
            MaterialCost = source.MaterialCost,
            PaperSheetSizeAlias = string.Empty,
            PaperSizeFormat = string.Empty,
            PrintsPerSheet = 0,
            PrintsPerPage = 0,
            PrintPerPageEx = string.Empty,
            PageWidth = 0,
            PageHeight = 0,
            TotalCostA = source.TotalCostA,
            TotalCostB = source.TotalCostA,
            TotalCostC = source.TotalCostA,
            TotalCostD = source.TotalCostA,
            UnitCostA = source.UnitCostA,
            UnitCostB = source.UnitCostA,
            UnitCostC = source.UnitCostA,
            UnitCostD = source.UnitCostA,
            Status = source.Status,
            ModifiedBy = Guid.Empty,
            ModifiedOn = source.QuotedOn,
            CreatedBy = Guid.Empty,
            CreatedOn = source.QuotedOn,
            Retired = false,
            RetiredBy = null,
            RetiredOn = null
        };
    }
}
