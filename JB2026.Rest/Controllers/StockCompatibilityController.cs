using System.Security.Claims;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
public sealed class StockCompatibilityController : ControllerBase
{
    private readonly IProductStoredProcedureGateway _productGateway;
    private readonly IStockInOutStoredProcedureGateway _stockInOutGateway;
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IConfiguration _configuration;

    public StockCompatibilityController(
        IProductStoredProcedureGateway productGateway,
        IStockInOutStoredProcedureGateway stockInOutGateway,
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IConfiguration configuration)
    {
        _productGateway = productGateway;
        _stockInOutGateway = stockInOutGateway;
        _readContext = readContext;
        _writeContext = writeContext;
        _configuration = configuration;
    }

    [HttpGet("api/Product/{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productGateway.SelectAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("api/Product/Created/{starton:datetime}/{max:int}")]
    public async Task<IActionResult> GetProductCreated(DateTime starton, int max, CancellationToken cancellationToken)
    {
        var upper = starton.Date.AddDays(1);
        var limit = max <= 0 ? 50 : Math.Min(max, 500);

        var list = await _readContext.vwProductLists
            .AsNoTracking()
            .Where(x => !x.Retired && x.CreatedOn < upper)
            .OrderByDescending(x => x.CreatedOn)
            .Take(limit)
            .OrderBy(x => x.StockNumber)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/Product/Modified/{starton:datetime}/{max:int}")]
    public async Task<IActionResult> GetProductModified(DateTime starton, int max, CancellationToken cancellationToken)
    {
        var upper = starton.Date.AddDays(1);
        var limit = max <= 0 ? 50 : Math.Min(max, 500);

        var list = await _readContext.vwProductLists
            .AsNoTracking()
            .Where(x => !x.Retired && x.ModifiedOn < upper && x.CreatedOn != x.ModifiedOn)
            .OrderByDescending(x => x.ModifiedOn)
            .Take(limit)
            .OrderBy(x => x.StockNumber)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/Product/Keyword/{keyword}")]
    public async Task<IActionResult> GetProductByKeyword(string keyword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 3)
        {
            return Ok(Array.Empty<object>());
        }

        var list = await _readContext.vwProductLists
            .AsNoTracking()
            .Where(x =>
                !x.Retired
                && ((x.StockNumber ?? string.Empty).Contains(keyword)
                    || (x.ProductCode ?? string.Empty).Contains(keyword)
                    || (x.ProductName ?? string.Empty).Contains(keyword)
                    || (x.Description ?? string.Empty).Contains(keyword)))
            .OrderBy(x => x.StockNumber)
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpGet("api/Product/Picture/{id:guid}")]
    public async Task<IActionResult> GetProductPicture(Guid id, CancellationToken cancellationToken)
    {
        var file = await ResolveProductPictureFileAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var content = await System.IO.File.ReadAllBytesAsync(file.Value.Path, cancellationToken);
        return File(content, GetContentType(file.Value.Path));
    }

    [HttpGet("api/Product/Thumbnail/{id:guid}/{width:int}/{height:int}")]
    public async Task<IActionResult> GetProductThumbnail(Guid id, int width = 100, int height = 100, CancellationToken cancellationToken = default)
    {
        _ = width;
        _ = height;

        var file = await ResolveProductPictureFileAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var content = await System.IO.File.ReadAllBytesAsync(file.Value.Path, cancellationToken);
        return File(content, GetContentType(file.Value.Path));
    }

    [HttpPost("api/Product/Picture/{id:guid}")]
    public async Task<IActionResult> PostProductPicture(Guid id, CancellationToken cancellationToken)
    {
        var productRoot = _configuration["LegacyFiles:ProductPictureRoot"];
        if (string.IsNullOrWhiteSpace(productRoot))
        {
            return MissingLegacyPathResponse("LegacyFiles:ProductPictureRoot");
        }

        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var product = await _readContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == id, cancellationToken);

        if (product is null || string.IsNullOrWhiteSpace(product.StockNumber))
        {
            return NotFound();
        }

        var upload = Request.Form.Files[0];
        var targetDirectory = Path.Combine(productRoot, product.StockNumber);
        Directory.CreateDirectory(targetDirectory);

        var fileName = Path.GetFileName(upload.FileName);
        var targetPath = Path.Combine(targetDirectory, fileName);

        await using (var stream = System.IO.File.Create(targetPath))
        {
            await upload.CopyToAsync(stream, cancellationToken);
        }

        var attachment = await _writeContext.ProductAttachments
            .OrderBy(x => x.AttachmentIndex)
            .FirstOrDefaultAsync(x => x.ProductId == id, cancellationToken);

        if (attachment is null)
        {
            _writeContext.ProductAttachments.Add(new ProductAttachment
            {
                AttachmentId = Guid.NewGuid(),
                ProductId = id,
                AttachmentIndex = 0,
                OriginalFileName = fileName
            });
        }
        else
        {
            attachment.AttachmentIndex = 0;
            attachment.OriginalFileName = fileName;
        }

        await _writeContext.SaveChangesAsync(cancellationToken);

        return Ok(new { ProductId = id, FileName = fileName });
    }

    [HttpPost("api/Product/StockInOut")]
    public async Task<IActionResult> PostProductStockInOut([FromBody] StockInOutCompatibilityRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
        {
            return BadRequest("ProductId is required");
        }

        var product = await _productGateway.SelectAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var actor = ResolveActorId();
        var now = DateTime.Now;

        await _stockInOutGateway.InsertAsync(new CreateStockInOutStoredProcedureRequest(
            ProductId: request.ProductId,
            InOutDate: now,
            Reference: request.Notes,
            Qty: request.Qty,
            CreatedOn: now,
            CreatedBy: actor,
            ModifiedOn: now,
            ModifiedBy: actor), cancellationToken);

        await _productGateway.UpdateAsync(new UpdateProductStoredProcedureRequest(
            ProductId: product.ProductId,
            CategoryId: product.CategoryId,
            StockNumber: product.StockNumber,
            ProductCode: product.ProductCode,
            ProductName: product.ProductName,
            Description: product.Description,
            Remarks: product.Remarks,
            MOQ: product.MOQ,
            Balance: product.Balance + request.Qty,
            SellingPrice: product.SellingPrice,
            COGS: product.COGS,
            CreatedOn: product.CreatedOn,
            CreatedBy: product.CreatedBy,
            ModifiedOn: now,
            ModifiedBy: actor,
            Retired: product.Retired,
            RetiredOn: product.RetiredOn,
            RetiredBy: product.RetiredBy), cancellationToken);

        return Ok(new { request.ProductId, request.Qty });
    }

    private Guid ResolveActorId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(candidate, out var actor) ? actor : Guid.Empty;
    }

    private async Task<(string Path, string FileName)?> ResolveProductPictureFileAsync(Guid productId, CancellationToken cancellationToken)
    {
        var productRoot = _configuration["LegacyFiles:ProductPictureRoot"];
        if (string.IsNullOrWhiteSpace(productRoot))
        {
            return null;
        }

        var product = await _readContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (product is null || string.IsNullOrWhiteSpace(product.StockNumber))
        {
            return null;
        }

        var attachment = await _readContext.ProductAttachments
            .AsNoTracking()
            .OrderBy(x => x.AttachmentIndex)
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        if (attachment is null || string.IsNullOrWhiteSpace(attachment.OriginalFileName))
        {
            return null;
        }

        var path = Path.Combine(productRoot, product.StockNumber, attachment.OriginalFileName);
        return System.IO.File.Exists(path) ? (path, attachment.OriginalFileName) : null;
    }

    private static string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private static ObjectResult MissingLegacyPathResponse(string key)
    {
        return new ObjectResult(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = $"Set configuration key '{key}' to enable this endpoint.",
            Status = StatusCodes.Status501NotImplemented
        })
        {
            StatusCode = StatusCodes.Status501NotImplemented
        };
    }

    public sealed record StockInOutCompatibilityRequest(Guid ProductId, int Qty, string? Notes);
}
