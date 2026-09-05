using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/sml")]
public sealed class SmlController : ControllerBase
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly ILogger<SmlController> _logger;
    private readonly JB5LegacyReadContext? _readContext;

    public SmlController(IQuotationRepository quotationRepository, ILogger<SmlController> logger, JB5LegacyReadContext? readContext = null)
    {
        _quotationRepository = quotationRepository;
        _logger = logger;
        _readContext = readContext;
    }

    [HttpGet("rtf-list")]
    [ProducesResponseType(typeof(SmlRtfListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmlRtfListResponse>> GetRtfList(
        [FromQuery] string? lookup,
        [FromQuery] int commonQuery = 1,
        [FromQuery] string? shortcut = "All",
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        if (_readContext is null)
        {
            return Problem("SML RTF list data source is unavailable.");
        }

        if (commonQuery is < 0 or > 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(commonQuery)] = ["Common query must be between 0 and 3."]
            }));
        }

        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var normalizedLookup = lookup?.Trim();
        var normalizedShortcut = string.IsNullOrWhiteSpace(shortcut) ? "All" : shortcut.Trim();

        try
        {
            var headers = await LoadLegacyHeadersAsync(normalizedLookup, normalizedShortcut, commonQuery, take, cancellationToken);

            if (headers.Count == 0)
            {
                headers = await LoadTableHeadersAsync(normalizedLookup, normalizedShortcut, commonQuery, take, cancellationToken);
            }

            var headerIds = headers.Select(header => header.HeaderId).ToArray();
            var headerIdSet = headerIds.ToHashSet();
            var purchaseOrders = new HashSet<string>(
                headers
                    .Select(header => header.PurchaseOrder ?? string.Empty)
                    .Where(purchaseOrder => !string.IsNullOrWhiteSpace(purchaseOrder)),
                StringComparer.OrdinalIgnoreCase);

        var dnCounts = new Dictionary<Guid, int>();
        try
        {
            dnCounts = await _readContext.SmlRtfExtractToDNs
                .AsNoTracking()
                .GroupBy(row => row.HeaderId)
                .Select(group => new { HeaderId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.HeaderId, item => item.Count, cancellationToken);

            dnCounts = dnCounts
                .Where(entry => headerIdSet.Contains(entry.Key))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load SML RTF DN counts; continuing with defaults.");
        }

        var invoiceInfo = new Dictionary<Guid, (int InvoiceCount, string InvoiceNumber)>();
        try
        {
            var invoiceItems = await _readContext.InvoiceItems
                .AsNoTracking()
                .Where(row => row.SmlRtfHeaderId.HasValue)
                .Select(row => new
                {
                    HeaderId = row.HeaderId,
                    SmlRtfHeaderId = row.SmlRtfHeaderId!.Value,
                })
                .ToListAsync(cancellationToken);

            var invoices = await _readContext.vwInvoiceLists
                .AsNoTracking()
                .Select(invoice => new
                {
                    invoice.HeaderId,
                    invoice.InvoiceNumber,
                    invoice.InvoiceDate,
                })
                .ToListAsync(cancellationToken);

            invoiceInfo = invoiceItems
                .Where(item => headerIdSet.Contains(item.SmlRtfHeaderId))
                .Join(
                    invoices,
                    item => item.HeaderId,
                    invoice => invoice.HeaderId,
                    (item, invoice) => new
                    {
                        item.SmlRtfHeaderId,
                        invoice.InvoiceNumber,
                        invoice.InvoiceDate,
                    })
                .GroupBy(item => item.SmlRtfHeaderId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var invoiceNumber = group
                            .OrderBy(item => item.InvoiceDate)
                            .Select(item => item.InvoiceNumber ?? string.Empty)
                            .FirstOrDefault() ?? string.Empty;

                        return (group.Count(), invoiceNumber);
                    });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load SML RTF invoice details; continuing with defaults.");
        }

            var detailRows = await LoadLegacyItemsAsync(cancellationToken);

            detailRows = detailRows
                .Where(item => purchaseOrders.Contains(item.PurchaseOrder ?? string.Empty))
                .OrderBy(item => item.PurchaseOrder)
                .ThenBy(item => item.LineNumber)
                .ToList();

            var itemsByPurchaseOrder = detailRows
            .GroupBy(item => item.PurchaseOrder ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SmlRtfListItemResponse>)group
                    .Select(item => new SmlRtfListItemResponse
                    {
                        LineNumber = item.LineNumber,
                        ProductCode = item.ProductCode ?? string.Empty,
                        ProductDescription = item.ProductDescription ?? string.Empty,
                        Price = item.Price ?? string.Empty,
                        Qty = item.Qty ?? string.Empty,
                        Amount = item.Amount ?? string.Empty,
                    })
                    .ToArray());

            var payloadHeaders = headers
            .Select((header, index) =>
            {
                var dnCount = dnCounts.GetValueOrDefault(header.HeaderId);
                var invoice = invoiceInfo.GetValueOrDefault(header.HeaderId);

                return new SmlRtfListHeaderResponse
                {
                    HeaderId = header.HeaderId,
                    RtfFileName = header.RtfFileName ?? string.Empty,
                    PurchaseOrder = header.PurchaseOrder ?? string.Empty,
                    RowNumber = index + 1,
                    CustomerPO = header.CustomerPO ?? string.Empty,
                    OrderedBy = header.OrderedBy ?? string.Empty,
                    OrderedOn = header.OrderedOn,
                    OriginalPO = header.OriginalPO ?? string.Empty,
                    SalesOrder = header.SalesOrder ?? string.Empty,
                    OriginalSO = header.OriginalSO ?? string.Empty,
                    DNCount = dnCount,
                    InvoiceCount = invoice.InvoiceCount,
                    InvoiceNumber = invoice.InvoiceNumber,
                    IsLabelPrinted = dnCount > 0,
                    CreatedOn = header.CreatedOn,
                    CreatedBy = header.CreatedByText,
                    Items = itemsByPurchaseOrder.GetValueOrDefault(header.PurchaseOrder ?? string.Empty) ?? Array.Empty<SmlRtfListItemResponse>(),
                };
            })
            .ToArray();

        _logger.LogInformation(
            "Returned SML RTF list with {Count} headers for lookup '{Lookup}', commonQuery {CommonQuery}, shortcut '{Shortcut}'",
            payloadHeaders.Length,
            normalizedLookup ?? string.Empty,
            commonQuery,
            normalizedShortcut);

        return Ok(new SmlRtfListResponse
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            RowCount = payloadHeaders.Length,
            Headers = payloadHeaders,
        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load SML RTF list for lookup '{Lookup}', commonQuery {CommonQuery}, shortcut '{Shortcut}'",
                normalizedLookup ?? string.Empty,
                commonQuery,
                normalizedShortcut);

            return Ok(new SmlRtfListResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = 0,
                Headers = Array.Empty<SmlRtfListHeaderResponse>(),
            });
        }
    }

    private static IEnumerable<SmlRtfHeaderViewRow> ApplyInMemoryFilter(
        IEnumerable<SmlRtfHeaderViewRow> rows,
        string? normalizedLookup,
        string normalizedShortcut,
        int commonQuery)
    {
        var query = rows;

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            return query.Where(header =>
                (header.PurchaseOrder ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (header.CustomerPO ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (header.OriginalPO ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (header.SalesOrder ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (header.OriginalSO ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase));
        }

        if (!normalizedShortcut.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedShortcut == "9")
            {
                query = query.Where(header =>
                {
                    var po = header.PurchaseOrder ?? string.Empty;
                    return po.Length == 0 || !char.IsLetter(po[0]);
                });
            }
            else
            {
                var firstChar = normalizedShortcut[..1];
                query = query.Where(header => (header.PurchaseOrder ?? string.Empty).StartsWith(firstChar, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (commonQuery is >= 1 and <= 3)
        {
            var now = DateTime.Now;
            var upperBound = now.Date.AddDays(1);
            var lowerBound = commonQuery switch
            {
                1 => now.Date.AddDays(-30),
                2 => now.Date.AddDays(-60),
                3 => now.Date.AddDays(-90),
                _ => now.Date.AddDays(-30),
            };

            query = query.Where(header => header.CreatedOn <= upperBound && header.CreatedOn >= lowerBound);
        }

        return query;
    }

    private async Task<List<SmlRtfHeaderMaterialized>> LoadLegacyHeadersAsync(
        string? normalizedLookup,
        string normalizedShortcut,
        int commonQuery,
        int take,
        CancellationToken cancellationToken)
    {
        var activeViewRows = await _readContext!.Database.SqlQueryRaw<SmlRtfHeaderViewRow>(@"
SELECT
    [HeaderId],
    [RtfFileName],
    [PurchaseOrder],
    [CustomerPO],
    [OrderedBy],
    [OrderedOn],
    [OriginalPO],
    [SalesOrder],
    [OriginalSO],
    [CreatedOn],
    [CreatedBy]
FROM [dbo].[vwRtfHeaderList_Active]")
            .ToListAsync(cancellationToken);

        var activeHeaders = ApplyInMemoryFilter(activeViewRows, normalizedLookup, normalizedShortcut, commonQuery)
            .OrderByDescending(row => row.PurchaseOrder)
            .Take(take)
            .Select(MapLegacyHeader)
            .ToList();

        if (activeHeaders.Count > 0)
        {
            return activeHeaders;
        }

        var fullViewRows = await _readContext.Database.SqlQueryRaw<SmlRtfHeaderViewRow>(@"
SELECT
    [HeaderId],
    [RtfFileName],
    [PurchaseOrder],
    [CustomerPO],
    [OrderedBy],
    [OrderedOn],
    [OriginalPO],
    [SalesOrder],
    [OriginalSO],
    [CreatedOn],
    [CreatedBy]
FROM [dbo].[vwRtfHeaderList]")
            .ToListAsync(cancellationToken);

        return ApplyInMemoryFilter(fullViewRows, normalizedLookup, normalizedShortcut, commonQuery)
            .OrderByDescending(row => row.PurchaseOrder)
            .Take(take)
            .Select(MapLegacyHeader)
            .ToList();
    }

    private async Task<List<SmlRtfHeaderMaterialized>> LoadTableHeadersAsync(
        string? normalizedLookup,
        string normalizedShortcut,
        int commonQuery,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _readContext!.SmlRtfHeaders
            .AsNoTracking()
            .Where(header => !header.Retired);

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            query = query.Where(header =>
                (header.PurchaseOrder ?? string.Empty).Contains(normalizedLookup) ||
                (header.CustomerPO ?? string.Empty).Contains(normalizedLookup) ||
                (header.OriginalPO ?? string.Empty).Contains(normalizedLookup) ||
                (header.SalesOrder ?? string.Empty).Contains(normalizedLookup) ||
                (header.OriginalSO ?? string.Empty).Contains(normalizedLookup));
        }
        else
        {
            if (!normalizedShortcut.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (normalizedShortcut == "9")
                {
                    query = query.Where(header => !EF.Functions.Like(header.PurchaseOrder ?? string.Empty, "[A-Za-z]%"));
                }
                else
                {
                    var firstChar = normalizedShortcut[..1];
                    query = query.Where(header => EF.Functions.Like(header.PurchaseOrder ?? string.Empty, $"{firstChar}%"));
                }
            }

            if (commonQuery is >= 1 and <= 3)
            {
                var now = DateTime.Now;
                var upperBound = now.Date.AddDays(1);
                var lowerBound = commonQuery switch
                {
                    1 => now.Date.AddDays(-30),
                    2 => now.Date.AddDays(-60),
                    3 => now.Date.AddDays(-90),
                    _ => now.Date.AddDays(-30),
                };

                query = query.Where(header => header.CreatedOn <= upperBound && header.CreatedOn >= lowerBound);
            }
        }

        return await query
            .OrderByDescending(header => header.PurchaseOrder)
            .Take(take)
            .Select(header => new SmlRtfHeaderMaterialized
            {
                HeaderId = header.HeaderId,
                RtfFileName = header.RtfFileName,
                PurchaseOrder = header.PurchaseOrder,
                CustomerPO = header.CustomerPO,
                OrderedBy = header.OrderedBy,
                OrderedOn = header.OrderedOn,
                OriginalPO = header.OriginalPO,
                SalesOrder = header.SalesOrder,
                OriginalSO = header.OriginalSO,
                CreatedOn = header.CreatedOn,
                CreatedByText = header.CreatedBy.HasValue ? header.CreatedBy.Value.ToString() : string.Empty,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SmlRtfItemViewRow>> LoadLegacyItemsAsync(CancellationToken cancellationToken)
    {
        return await _readContext!.Database.SqlQueryRaw<SmlRtfItemViewRow>(@"
SELECT
    [PurchaseOrder],
    [LineNumber],
    [ProductCode],
    [ProductDescription],
    [Price],
    [Discount],
    [Qty],
    [Amount]
FROM [dbo].[vwRtfItemList]")
            .ToListAsync(cancellationToken);
    }

    private static SmlRtfHeaderMaterialized MapLegacyHeader(SmlRtfHeaderViewRow row)
    {
        return new SmlRtfHeaderMaterialized
        {
            HeaderId = row.HeaderId,
            RtfFileName = row.RtfFileName,
            PurchaseOrder = row.PurchaseOrder,
            CustomerPO = row.CustomerPO,
            OrderedBy = row.OrderedBy,
            OrderedOn = row.OrderedOn,
            OriginalPO = row.OriginalPO,
            SalesOrder = row.SalesOrder,
            OriginalSO = row.OriginalSO,
            CreatedOn = row.CreatedOn,
            CreatedByText = row.CreatedBy ?? string.Empty,
        };
    }

    private sealed class SmlRtfHeaderMaterialized
    {
        public Guid HeaderId { get; init; }

        public string? RtfFileName { get; init; }

        public string? PurchaseOrder { get; init; }

        public string? CustomerPO { get; init; }

        public string? OrderedBy { get; init; }

        public DateTime OrderedOn { get; init; }

        public string? OriginalPO { get; init; }

        public string? SalesOrder { get; init; }

        public string? OriginalSO { get; init; }

        public DateTime CreatedOn { get; init; }

        public string CreatedByText { get; init; } = string.Empty;
    }

    private sealed class SmlRtfHeaderViewRow
    {
        public Guid HeaderId { get; init; }

        public string? RtfFileName { get; init; }

        public string? PurchaseOrder { get; init; }

        public string? CustomerPO { get; init; }

        public string? OrderedBy { get; init; }

        public DateTime OrderedOn { get; init; }

        public string? OriginalPO { get; init; }

        public string? SalesOrder { get; init; }

        public string? OriginalSO { get; init; }

        public DateTime CreatedOn { get; init; }

        public string? CreatedBy { get; init; }
    }

    private sealed class SmlRtfItemViewRow
    {
        public string? PurchaseOrder { get; init; }

        public int LineNumber { get; init; }

        public string? ProductCode { get; init; }

        public string? ProductDescription { get; init; }

        public string? Price { get; init; }

        public string? Discount { get; init; }

        public string? Qty { get; init; }

        public string? Amount { get; init; }
    }

    [HttpGet("rtf-stats")]
    [ProducesResponseType(typeof(SmlRtfStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmlRtfStatsResponse>> GetRtfStats(
        [FromQuery] DateOnly? startOn,
        [FromQuery] DateOnly? endOn,
        [FromQuery] string? lookup,
        [FromQuery] int take = 5000,
        CancellationToken cancellationToken = default)
    {
        if (_readContext is null)
        {
            return Problem("SML RTF stats data source is unavailable.");
        }

        if (take is <= 0 or > 20000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 20000."]
            }));
        }

        if (startOn.HasValue && endOn.HasValue && startOn.Value > endOn.Value)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(startOn)] = ["Start date cannot be later than end date."],
                [nameof(endOn)] = ["End date cannot be earlier than start date."],
            }));
        }

        var normalizedLookup = lookup?.Trim();

        try
        {
            IQueryable<JB2026.EfCore.Models.vwOlapSmlRtf> dbQuery = _readContext.vwOlapSmlRtfs.AsNoTracking();

            if (startOn.HasValue)
            {
                var startDate = startOn.Value.ToDateTime(TimeOnly.MinValue);
                dbQuery = dbQuery.Where(row => row.OrderedOn >= startDate);
            }

            if (endOn.HasValue)
            {
                var endDateExclusive = endOn.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
                dbQuery = dbQuery.Where(row => row.OrderedOn < endDateExclusive);
            }

            var sourceRows = await dbQuery.ToListAsync(cancellationToken);

            var memQuery = sourceRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(normalizedLookup))
            {
                memQuery = memQuery.Where(row =>
                    (row.PurchaseOrder ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    row.CustomerPO.Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.OrderedBy ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.OriginalPO ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.SalesOrder ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    row.OriginalSO.Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.ProductCode ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase));
            }

            var rows = memQuery
                .OrderByDescending(row => row.OrderedOn)
                .ThenBy(row => row.PurchaseOrder)
                .ThenBy(row => row.ProductCode)
                .Take(take)
                .Select(row => new SmlRtfStatsRowResponse
                {
                    PurchaseOrder = row.PurchaseOrder ?? string.Empty,
                    CustomerPO = row.CustomerPO,
                    OrderedOn = row.OrderedOn,
                    OrderedBy = row.OrderedBy ?? string.Empty,
                    OriginalPO = row.OriginalPO ?? string.Empty,
                    SalesOrder = row.SalesOrder ?? string.Empty,
                    OriginalSO = row.OriginalSO,
                    ProductCode = row.ProductCode ?? string.Empty,
                    Price = row.Price ?? string.Empty,
                    Qty = row.Qty ?? string.Empty,
                    Year = row.OrderedOn.Year,
                    Month = row.OrderedOn.Month,
                    Amount = row.Amount ?? 0m,
                })
                .ToArray();

            return Ok(new SmlRtfStatsResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = rows.Length,
                Rows = rows,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load SML RTF stats for lookup '{Lookup}', start {StartOn}, end {EndOn}",
                normalizedLookup ?? string.Empty,
                startOn,
                endOn);

            return Ok(new SmlRtfStatsResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = 0,
                Rows = Array.Empty<SmlRtfStatsRowResponse>(),
            });
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(SmlStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<SmlStatsResponse> GetStats([FromQuery] DateOnly startOn, [FromQuery] int days = 31, [FromQuery] int take = 500)
    {
        if (days is <= 0 or > 31)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(days)] = ["Days must be between 1 and 31."]
            }));
        }

        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var rows = _quotationRepository
            .GetRange(startOn, days)
            .Take(take)
            .ToArray();

        var monthly = rows
            .GroupBy(row => new { row.QuotedOn.Year, row.QuotedOn.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new SmlMonthlyStatResponse
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Count = group.Count(),
                Amount = group.Sum(item => item.TotalCostA),
            })
            .ToArray();

        var topCustomers = rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.CustomerName) ? "Unknown" : row.CustomerName)
            .Select(group => new SmlTopCustomerResponse
            {
                CustomerName = group.Key,
                Count = group.Count(),
                Amount = group.Sum(item => item.TotalCostA),
            })
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.CustomerName)
            .Take(5)
            .ToArray();

        _logger.LogInformation("Computed SML stats with {Rows} rows for {StartOn}+{Days}", rows.Length, startOn, days);

        return Ok(new SmlStatsResponse
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            RowCount = rows.Length,
            TotalAmount = rows.Sum(row => row.TotalCostA),
            Monthly = monthly,
            TopCustomers = topCustomers,
        });
    }

    [HttpGet("invoice-stats")]
    [ProducesResponseType(typeof(SmlInvoiceStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmlInvoiceStatsResponse>> GetInvoiceStats(
        [FromQuery] DateOnly? startOn,
        [FromQuery] DateOnly? endOn,
        [FromQuery] string? lookup,
        [FromQuery] int? take,
        CancellationToken cancellationToken = default)
    {
        if (_readContext is null)
        {
            return Problem("SML invoice stats data source is unavailable.");
        }

        if (take.HasValue && take.Value <= 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be greater than 0."]
            }));
        }

        if (startOn.HasValue && endOn.HasValue && startOn.Value > endOn.Value)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(startOn)] = ["Start date cannot be later than end date."],
                [nameof(endOn)] = ["End date cannot be earlier than start date."],
            }));
        }

        var normalizedLookup = lookup?.Trim();

        try
        {
            IQueryable<JB2026.EfCore.Models.vwOlapInvoiceStat> dbQuery = _readContext.vwOlapInvoiceStats.AsNoTracking();

            if (startOn.HasValue)
            {
                dbQuery = dbQuery.Where(row => row.InvoiceDate.HasValue && row.InvoiceDate.Value >= startOn.Value);
            }

            if (endOn.HasValue)
            {
                dbQuery = dbQuery.Where(row => row.InvoiceDate.HasValue && row.InvoiceDate.Value <= endOn.Value);
            }

            var sourceRows = await dbQuery.ToListAsync(cancellationToken);
            var memQuery = sourceRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(normalizedLookup))
            {
                memQuery = memQuery.Where(row =>
                    (row.CustomerName ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.InvoiceNumber ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.CreatedBy ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.PurchaseOrder ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.ProductCode ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                    (row.Unit ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase));
            }

            IEnumerable<JB2026.EfCore.Models.vwOlapInvoiceStat> orderedRows = memQuery
                .OrderByDescending(row => row.InvoiceDate)
                .ThenBy(row => row.CustomerName)
                .ThenBy(row => row.InvoiceNumber)
                .ThenBy(row => row.PurchaseOrder)
                .ThenBy(row => row.ProductCode);

            if (take.HasValue)
            {
                orderedRows = orderedRows.Take(take.Value);
            }

            var rows = orderedRows
                .Select(row => new SmlInvoiceStatsRowResponse
                {
                    CustomerName = row.CustomerName ?? string.Empty,
                    InvoiceNumber = row.InvoiceNumber ?? string.Empty,
                    InvoiceDate = row.InvoiceDate,
                    InvoiceAmount = row.InvoiceAmount ?? 0m,
                    CreatedOn = row.CreatedOn,
                    CreatedBy = row.CreatedBy ?? string.Empty,
                    PurchaseOrder = row.PurchaseOrder ?? string.Empty,
                    ProductCode = row.ProductCode ?? string.Empty,
                    Qty = row.Qty ?? 0m,
                    Unit = row.Unit ?? string.Empty,
                    Price = row.Price ?? 0m,
                    Amount = row.Amount ?? 0m,
                    Year = row.InvoiceDate?.Year ?? 0,
                    Month = row.InvoiceDate?.Month ?? 0,
                })
                .ToArray();

            return Ok(new SmlInvoiceStatsResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = rows.Length,
                Rows = rows,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load SML invoice stats for lookup '{Lookup}', start {StartOn}, end {EndOn}",
                normalizedLookup ?? string.Empty,
                startOn,
                endOn);

            return Ok(new SmlInvoiceStatsResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = 0,
                Rows = Array.Empty<SmlInvoiceStatsRowResponse>(),
            });
        }
    }

    [HttpGet("invoice-list")]
    [ProducesResponseType(typeof(SmlInvoiceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmlInvoiceListResponse>> GetInvoiceList(
        [FromQuery] string? lookup,
        [FromQuery] int commonQuery = 1,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        if (_readContext is null)
        {
            return Problem("SML invoice list data source is unavailable.");
        }

        if (commonQuery is < 0 or > 3)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(commonQuery)] = ["Common query must be between 0 and 3."]
            }));
        }

        if (take is <= 0 or > 1000)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 1000."]
            }));
        }

        var normalizedLookup = lookup?.Trim();

        try
        {
            var sourceRows = await _readContext.Database.SqlQueryRaw<SmlInvoiceListViewRow>(@"
SELECT
    [HeaderId],
    [InvoiceNumber],
    [CustomerName],
    [InvoiceDate],
    [InvoiceAmount],
    [ICNumber],
    [CreatedOn],
    [CreatedBy],
    [ModifiedOn],
    [ModifiedBy]
FROM [dbo].[vwInvoiceList]")
                .ToListAsync(cancellationToken);

            var filtered = ApplyInvoiceListFilter(sourceRows, normalizedLookup, commonQuery)
                .OrderByDescending(row => row.InvoiceNumber ?? string.Empty)
                .Take(take)
                .ToArray();

            // Get HeaderIds for line item query
            var headerIdSet = filtered.Select(row => row.HeaderId).ToHashSet();

            // Query line items from InvoiceItem and InvoiceSubItem tables
            var lineItemsLookup = new Dictionary<Guid, IReadOnlyList<SmlInvoiceListItemResponse>>();
            
            try
            {
                // EF Core 8 translates Contains() to OPENJSON where '$' gets parameterized.
                // Load all items and filter in-memory.
                var allLineItems = await _readContext.InvoiceItems
                    .AsNoTracking()
                    .Include(item => item.InvoiceSubItems)
                    .ToListAsync(cancellationToken);

                var lineItemsByHeaderId = allLineItems
                    .Where(item => headerIdSet.Contains(item.HeaderId))
                    .SelectMany(item => item.InvoiceSubItems.Select(subItem => new
                    {
                        HeaderId = item.HeaderId,
                        LineNumber = item.LineNumber,
                        SubLineNumber = subItem.SubLineNumber,
                        Description = subItem.Description ?? string.Empty,
                        Quantity = subItem.Quantity ?? 0m,
                        Unit = subItem.UoM ?? string.Empty,
                        Price = subItem.Price ?? 0m,
                        Amount = subItem.Amount ?? 0m
                    }))
                    .OrderBy(x => x.HeaderId)
                    .ThenBy(x => x.LineNumber)
                    .ThenBy(x => x.SubLineNumber)
                    .ToList();

                // Group line items by HeaderId
                lineItemsLookup = lineItemsByHeaderId
                    .GroupBy(x => x.HeaderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new SmlInvoiceListItemResponse
                        {
                            LineNumber = x.LineNumber,
                            Description = x.Description,
                            Quantity = x.Quantity,
                            Unit = x.Unit,
                            Price = x.Price,
                            Amount = x.Amount
                        })
                        .ToList() as IReadOnlyList<SmlInvoiceListItemResponse>);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load line items for invoice list");
                // Continue without items if query fails
            }

            var payloadRows = filtered
                .Select((row, index) => new SmlInvoiceListRowResponse
                {
                    HeaderId = row.HeaderId,
                    InvoiceNumber = row.InvoiceNumber ?? string.Empty,
                    RowNumber = index + 1,
                    CustomerName = row.CustomerName ?? string.Empty,
                    InvoiceDate = row.InvoiceDate,
                    InvoiceAmount = row.InvoiceAmount ?? 0m,
                    ICNumber = row.ICNumber ?? string.Empty,
                    CreatedOn = row.ModifiedOn,
                    CreatedBy = row.ModifiedBy ?? row.CreatedBy ?? string.Empty,
                    Items = lineItemsLookup.TryGetValue(row.HeaderId, out var items)
                        ? items
                        : new List<SmlInvoiceListItemResponse>()
                })
                .ToArray();

            _logger.LogInformation(
                "Returned SML Invoice list with {Count} rows for lookup '{Lookup}' and commonQuery {CommonQuery}",
                payloadRows.Length,
                normalizedLookup ?? string.Empty,
                commonQuery);

            return Ok(new SmlInvoiceListResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = payloadRows.Length,
                Rows = payloadRows,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to load SML Invoice list for lookup '{Lookup}' and commonQuery {CommonQuery}",
                normalizedLookup ?? string.Empty,
                commonQuery);

            return Ok(new SmlInvoiceListResponse
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                RowCount = 0,
                Rows = Array.Empty<SmlInvoiceListRowResponse>(),
            });
        }
    }

    private static IEnumerable<SmlInvoiceListViewRow> ApplyInvoiceListFilter(
        IEnumerable<SmlInvoiceListViewRow> rows,
        string? normalizedLookup,
        int commonQuery)
    {
        var query = rows;

        if (!string.IsNullOrWhiteSpace(normalizedLookup))
        {
            return query.Where(row =>
                (row.InvoiceNumber ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (row.CustomerName ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                (row.ICNumber ?? string.Empty).Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                row.InvoiceDate.ToString("yyyy-MM-dd").Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase) ||
                row.CreatedOn.ToString("yyyy-MM-dd").Contains(normalizedLookup, StringComparison.OrdinalIgnoreCase));
        }

        if (commonQuery is >= 1 and <= 3)
        {
            var now = DateTime.Now;
            var upperBound = now.Date.AddDays(1);
            var lowerBound = commonQuery switch
            {
                1 => now.Date.AddDays(-30),
                2 => now.Date.AddDays(-60),
                3 => now.Date.AddDays(-90),
                _ => now.Date.AddDays(-30),
            };

            query = query.Where(row => row.CreatedOn <= upperBound && row.CreatedOn >= lowerBound);
        }

        return query;
    }

    private sealed class SmlInvoiceListViewRow
    {
        public Guid HeaderId { get; init; }

        public string? InvoiceNumber { get; init; }

        public string? CustomerName { get; init; }

        public DateTime InvoiceDate { get; init; }

        public decimal? InvoiceAmount { get; init; }

        public string? ICNumber { get; init; }

        public DateTime CreatedOn { get; init; }

        public string? CreatedBy { get; init; }

        public DateTime ModifiedOn { get; init; }

        public string? ModifiedBy { get; init; }
    }
}