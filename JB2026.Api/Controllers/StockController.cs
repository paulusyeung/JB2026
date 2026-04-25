using JB2026.Api.Models;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/stock")]
public sealed class StockController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly ILogger<StockController> _logger;
    private readonly IConfiguration _configuration;

    public StockController(JB5LegacyReadContext readContext, ILogger<StockController> logger, IConfiguration configuration)
    {
        _readContext = readContext;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("products/{id:guid}")]
    [ProducesResponseType(typeof(StockProductRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockProductRecordResponse>> GetProductRecord(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _readContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductId == id && !item.Retired, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var categoryCode = string.Empty;
        if (product.CategoryId.HasValue)
        {
            categoryCode = await _readContext.Z_Categories
                .AsNoTracking()
                .Where(category => category.CategoryId == product.CategoryId.Value)
                .Select(category => category.CategoryCode ?? string.Empty)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Ok(MapProductRecord(product, categoryCode ?? string.Empty));
    }

    [HttpPost("products")]
    [ProducesResponseType(typeof(StockProductRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockProductRecordResponse>> CreateProductRecord(
        [FromBody] StockProductRecordUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var validation = ValidateUpsertRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var normalizedProductCode = request.ProductCode.Trim();
        var isCodeInUse = await _readContext.Products
            .AsNoTracking()
            .AnyAsync(product => !product.Retired && product.ProductCode == normalizedProductCode, cancellationToken);

        if (isCodeInUse)
        {
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ProductCode)] = ["Product code already exists."]
            }));
        }

        var now = DateTime.UtcNow;
        var actor = GetActorGuid();
        var categoryId = await ResolveCategoryIdAsync(request.CategoryCode, cancellationToken);
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            CategoryId = categoryId,
            StockNumber = ComposeStockNumber(request.CustomerCode, request.CategoryCode, request.SequenceNumber),
            ProductCode = normalizedProductCode,
            ProductName = request.ProductName.Trim(),
            Description = request.ProductionInfo?.Trim(),
            Remarks = request.Remarks?.Trim(),
            MOQ = 1,
            Balance = 0,
            SellingPrice = request.SellingPrice,
            COGS = request.COGS,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
            Retired = false,
            RetiredOn = null,
            RetiredBy = null
        };

        _readContext.Products.Add(product);
        await _readContext.SaveChangesAsync(cancellationToken);

        var response = MapProductRecord(product, request.CategoryCode);
        return CreatedAtAction(nameof(GetProductRecord), new { id = product.ProductId }, response);
    }

    [HttpPut("products/{id:guid}")]
    [ProducesResponseType(typeof(StockProductRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockProductRecordResponse>> UpdateProductRecord(
        Guid id,
        [FromBody] StockProductRecordUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var validation = ValidateUpsertRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        var product = await _readContext.Products.FirstOrDefaultAsync(item => item.ProductId == id && !item.Retired, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var normalizedProductCode = request.ProductCode.Trim();
        var isCodeInUse = await _readContext.Products
            .AsNoTracking()
            .AnyAsync(item => !item.Retired && item.ProductId != id && item.ProductCode == normalizedProductCode, cancellationToken);

        if (isCodeInUse)
        {
            return Conflict(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ProductCode)] = ["Product code already exists."]
            }));
        }

        product.CategoryId = await ResolveCategoryIdAsync(request.CategoryCode, cancellationToken);
        product.StockNumber = ComposeStockNumber(request.CustomerCode, request.CategoryCode, request.SequenceNumber);
        product.ProductCode = normalizedProductCode;
        product.ProductName = request.ProductName.Trim();
        product.Description = request.ProductionInfo?.Trim();
        product.Remarks = request.Remarks?.Trim();
        product.SellingPrice = request.SellingPrice;
        product.COGS = request.COGS;
        product.ModifiedOn = DateTime.UtcNow;
        product.ModifiedBy = GetActorGuid();

        await _readContext.SaveChangesAsync(cancellationToken);
        return Ok(MapProductRecord(product, request.CategoryCode));
    }

    [HttpDelete("products/{id:guid}")]
    [ProducesResponseType(typeof(StockProductDeleteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockProductDeleteResult>> DeleteProductRecord(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _readContext.Products
            .FirstOrDefaultAsync(item => item.ProductId == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (!product.Retired)
        {
            // First-pass: retire the product (soft delete)
            var actor = GetActorGuid();
            var now = DateTime.UtcNow;
            product.Retired = true;
            product.RetiredOn = now;
            product.RetiredBy = actor;
            product.ModifiedOn = now;
            product.ModifiedBy = actor;

            await _readContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} retired by {Actor}",
                product.ProductId, actor);

            return Ok(new StockProductDeleteResult
            {
                ProductId = product.ProductId,
                Outcome = "retired"
            });
        }

        // Second-pass: hard delete with cascading cleanup
        await HardDeleteProductAsync(product, cancellationToken);

        return Ok(new StockProductDeleteResult
        {
            ProductId = id,
            Outcome = "hardDeleted"
        });
    }

    private async Task HardDeleteProductAsync(Product product, CancellationToken cancellationToken)
    {
        // Remove stock in/out movement rows
        var stockMovements = await _readContext.StockInOuts
            .Where(item => item.ProductId == product.ProductId)
            .ToListAsync(cancellationToken);
        _readContext.StockInOuts.RemoveRange(stockMovements);

        // Remove product attachment rows and physical image files
        var attachments = await _readContext.ProductAttachments
            .Where(item => item.ProductId == product.ProductId)
            .ToListAsync(cancellationToken);

        var productPictureRoot = _configuration["LegacyFiles:ProductPictureRoot"];
        if (!string.IsNullOrWhiteSpace(productPictureRoot) && !string.IsNullOrWhiteSpace(product.StockNumber))
        {
            foreach (var attachment in attachments)
            {
                if (!string.IsNullOrWhiteSpace(attachment.OriginalFileName))
                {
                    var filePath = Path.Combine(productPictureRoot, product.StockNumber, attachment.OriginalFileName);
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to delete physical file {FilePath} for attachment {AttachmentId} during hard delete of product {ProductId}",
                            filePath, attachment.AttachmentId, product.ProductId);
                    }
                }
            }

            // Remove the product picture directory if empty after file cleanup
            var productDir = Path.Combine(productPictureRoot, product.StockNumber);
            try
            {
                if (Directory.Exists(productDir) && !Directory.EnumerateFiles(productDir).Any())
                {
                    Directory.Delete(productDir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to remove product picture directory {Dir} during hard delete of product {ProductId}",
                    productDir, product.ProductId);
            }
        }

        _readContext.ProductAttachments.RemoveRange(attachments);
        _readContext.Products.Remove(product);
        await _readContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product {ProductId} hard-deleted: {MovementCount} stock movements and {AttachmentCount} attachments removed",
            product.ProductId, stockMovements.Count, attachments.Count);
    }

    [HttpPost("products/{id:guid}/transactions")]
    [ProducesResponseType(typeof(StockInOutTransactionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockInOutTransactionResult>> CreateStockInOutTransaction(
        Guid id,
        [FromBody] StockInOutTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (request.Qty == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Qty)] = ["Quantity must be a non-zero signed integer."]
            }));
        }

        var product = await _readContext.Products
            .FirstOrDefaultAsync(item => item.ProductId == id && !item.Retired, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var actor = GetActorGuid();

        var transaction = new JB2026.EfCore.Models.StockInOut
        {
            InOutId = Guid.NewGuid(),
            ProductId = product.ProductId,
            InOutDate = request.InOutDate.Date,
            Reference = request.Reference?.Trim(),
            Qty = request.Qty,
            CreatedOn = now,
            CreatedBy = actor,
            ModifiedOn = now,
            ModifiedBy = actor,
        };

        _readContext.StockInOuts.Add(transaction);
        product.Balance += request.Qty;
        product.ModifiedOn = now;
        product.ModifiedBy = actor;

        await _readContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock in/out transaction {InOutId} created for product {ProductId} with qty {Qty}, new balance {Balance}",
            transaction.InOutId, product.ProductId, request.Qty, product.Balance);

        return CreatedAtAction(
            nameof(GetProductMovements),
            new { id = product.ProductId },
            new StockInOutTransactionResult
            {
                InOutId = transaction.InOutId,
                ProductId = product.ProductId,
                NewBalance = product.Balance,
            });
    }

    [HttpGet("products/{id:guid}/movements")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementHistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementHistoryItemResponse>>> GetProductMovements(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movements = await (
            from item in _readContext.StockInOuts.AsNoTracking()
            where item.ProductId == id
            join userInfo in _readContext.UserInfos.AsNoTracking() on item.ModifiedBy equals userInfo.UserId into userInfoGroup
            from userInfo in userInfoGroup
                .OrderByDescending(entry => entry.PrimaryRec)
                .Take(1)
                .DefaultIfEmpty()
            orderby item.InOutDate, item.CreatedOn
            select new
            {
                item.InOutId,
                item.InOutDate,
                item.Reference,
                item.Qty,
                item.ModifiedOn,
                item.ModifiedBy,
                UserName = userInfo != null ? userInfo.UserName : null,
                UserAlias = userInfo != null ? userInfo.UserAlias : null
            })
            .ToListAsync(cancellationToken);

        var runningBalance = 0;
        var result = movements.Select(item =>
        {
            runningBalance += item.Qty;
            var alias = (item.UserAlias ?? string.Empty).Trim();
            var name = (item.UserName ?? string.Empty).Trim();
            var displayName = string.IsNullOrWhiteSpace(alias) ? name : alias;

            return new StockMovementHistoryItemResponse
            {
                InOutId = item.InOutId,
                InOutDate = item.InOutDate,
                Reference = item.Reference ?? string.Empty,
                Qty = item.Qty,
                RunningBalance = runningBalance,
                ModifiedOn = item.ModifiedOn,
                ModifiedBy = string.IsNullOrWhiteSpace(displayName)
                    ? item.ModifiedBy.ToString("D")
                    : displayName
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("products/next-number")]
    [ProducesResponseType(typeof(StockProductNextNumberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockProductNextNumberResponse>> GetNextProductNumber(
        [FromQuery] string customerCode,
        [FromQuery] string categoryCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCustomer = (customerCode ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedCategory = (categoryCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCustomer) || string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(customerCode)] = ["Customer code is required."],
                [nameof(categoryCode)] = ["Category code is required."]
            }));
        }

        var prefix = $"{normalizedCustomer}-{normalizedCategory}-";
        var existing = await _readContext.Products
            .AsNoTracking()
            .Where(item => !item.Retired && item.StockNumber != null && item.StockNumber.StartsWith(prefix))
            .Select(item => item.StockNumber!)
            .ToListAsync(cancellationToken);

        var maxNumber = 0;
        foreach (var stockNumber in existing)
        {
            var sequence = stockNumber[(prefix.Length)..];
            if (int.TryParse(sequence, out var value) && value > maxNumber)
            {
                maxNumber = value;
            }
        }

        var nextSequence = (maxNumber + 1).ToString("0000");
        return Ok(new StockProductNextNumberResponse
        {
            CustomerCode = normalizedCustomer,
            CategoryCode = normalizedCategory,
            SequenceNumber = nextSequence,
            StockNumber = ComposeStockNumber(normalizedCustomer, normalizedCategory, nextSequence)
        });
    }

    [HttpGet("products/validate-code")]
    [ProducesResponseType(typeof(StockProductCodeValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockProductCodeValidationResponse>> ValidateProductCodeUniqueness(
        [FromQuery] string productCode,
        [FromQuery] Guid? excludeProductId,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = (productCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(productCode)] = ["Product code is required."]
            }));
        }

        var exists = await _readContext.Products
            .AsNoTracking()
            .AnyAsync(item =>
                !item.Retired
                && item.ProductCode == normalizedCode
                && (!excludeProductId.HasValue || item.ProductId != excludeProductId.Value),
                cancellationToken);

        return Ok(new StockProductCodeValidationResponse { IsUnique = !exists });
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<StockProductListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StockProductListItemResponse>>> GetProducts(
        [FromQuery] string? keyword,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 500)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(take)] = ["Take must be between 1 and 500."]
            }));
        }

        var normalizedKeyword = keyword?.Trim();

        var query = _readContext.vwProductLists
            .AsNoTracking()
            .Where(product => !product.Retired);

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            query = query.Where(product =>
                (product.StockNumber ?? string.Empty).Contains(normalizedKeyword) ||
                (product.ProductCode ?? string.Empty).Contains(normalizedKeyword) ||
                (product.ProductName ?? string.Empty).Contains(normalizedKeyword));
        }

        var products = await query
            .OrderBy(product => product.StockNumber)
            .Take(take)
            .Select(product => new StockProductListItemResponse
            {
                ProductId = product.ProductId,
                StockNumber = product.StockNumber ?? string.Empty,
                ProductCode = product.ProductCode ?? string.Empty,
                ProductName = product.ProductName ?? string.Empty,
                Balance = product.Balance,
                SellingPrice = product.SellingPrice,
                COGS = product.COGS,
                Remarks = product.Description ?? string.Empty,
                AttachmentCount = _readContext.ProductAttachments.Count(attachment => attachment.ProductId == product.ProductId),
                CreatedOn = product.CreatedOn,
                CreatedBy = product.CreatedBy ?? string.Empty,
                ModifiedOn = product.ModifiedOn,
                ModifiedBy = product.ModifiedBy ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Returned {Count} stock products for keyword '{Keyword}' with take {Take}", products.Count, normalizedKeyword ?? string.Empty, take);
        return Ok(products);
    }

    private ActionResult? ValidateUpsertRequest(StockProductRecordUpsertRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CustomerCode))
        {
            errors[nameof(request.CustomerCode)] = ["Customer code is required."];
        }

        if (string.IsNullOrWhiteSpace(request.CategoryCode))
        {
            errors[nameof(request.CategoryCode)] = ["Category code is required."];
        }

        if (string.IsNullOrWhiteSpace(request.SequenceNumber))
        {
            errors[nameof(request.SequenceNumber)] = ["Sequence number is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ProductCode))
        {
            errors[nameof(request.ProductCode)] = ["Product code is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            errors[nameof(request.ProductName)] = ["Product name is required."];
        }

        return errors.Count > 0
            ? BadRequest(new ValidationProblemDetails(errors))
            : null;
    }

    private async Task<Guid?> ResolveCategoryIdAsync(string categoryCode, CancellationToken cancellationToken)
    {
        var normalizedCategoryCode = categoryCode.Trim().ToUpperInvariant();
        return await _readContext.Z_Categories
            .AsNoTracking()
            .Where(category => !category.Retired && (category.CategoryCode ?? string.Empty).ToUpper() == normalizedCategoryCode)
            .Select(category => (Guid?)category.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static StockProductRecordResponse MapProductRecord(Product product, string categoryCode)
    {
        var segments = ParseStockNumber(product.StockNumber);
        var responseCategory = string.IsNullOrWhiteSpace(categoryCode) ? segments.categoryCode : categoryCode;

        return new StockProductRecordResponse
        {
            ProductId = product.ProductId,
            CustomerCode = segments.customerCode,
            CategoryCode = responseCategory,
            SequenceNumber = segments.sequenceNumber,
            StockNumber = product.StockNumber ?? string.Empty,
            ProductCode = product.ProductCode ?? string.Empty,
            ProductName = product.ProductName ?? string.Empty,
            ProductionInfo = product.Description ?? string.Empty,
            Remarks = product.Remarks ?? string.Empty,
            SellingPrice = product.SellingPrice,
            COGS = product.COGS,
            Balance = product.Balance,
            CreatedOn = product.CreatedOn,
            CreatedBy = product.CreatedBy.ToString("D"),
            ModifiedOn = product.ModifiedOn,
            ModifiedBy = product.ModifiedBy.ToString("D")
        };
    }

    private static string ComposeStockNumber(string customerCode, string categoryCode, string sequenceNumber)
    {
        var customer = customerCode.Trim().ToUpperInvariant();
        var category = categoryCode.Trim().ToUpperInvariant();
        var sequence = sequenceNumber.Trim().PadLeft(4, '0');
        return $"{customer}-{category}-{sequence}";
    }

    private static (string customerCode, string categoryCode, string sequenceNumber) ParseStockNumber(string? stockNumber)
    {
        if (string.IsNullOrWhiteSpace(stockNumber))
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var segments = stockNumber.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3)
        {
            return (segments[0], segments[1], segments[2]);
        }

        return (string.Empty, string.Empty, stockNumber);
    }

    private Guid GetActorGuid()
    {
        var raw = User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var userId))
        {
            return userId;
        }

        return Guid.NewGuid();
    }
}