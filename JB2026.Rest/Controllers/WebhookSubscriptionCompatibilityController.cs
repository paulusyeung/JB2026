using JB2026.EfCore.Data;
using JB2026.EfCore.Models;
using JB2026.Rest.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Rest.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WebhookSubscriptionController : ControllerBase
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;
    private readonly IWebhookDispatcherService _webhookDispatcher;

    public WebhookSubscriptionController(
        JB5LegacyReadContext readContext,
        JB5LegacyWriteContext writeContext,
        IWebhookDispatcherService webhookDispatcher)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _webhookDispatcher = webhookDispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WebhookSubscription>>> Get(CancellationToken cancellationToken)
    {
        var items = await _readContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WebhookSubscription>> Get(int id, CancellationToken cancellationToken)
    {
        var item = await _readContext.WebhookSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<WebhookSubscription>> Post([FromBody] WebhookSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.Url) || string.IsNullOrWhiteSpace(subscription.EventTypes))
        {
            return BadRequest("Invalid subscription parameters");
        }

        var item = new WebhookSubscription
        {
            Url = subscription.Url,
            EventTypes = subscription.EventTypes,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = null
        };

        _writeContext.WebhookSubscriptions.Add(item);
        await _writeContext.SaveChangesAsync(cancellationToken);

        await _webhookDispatcher.EnqueueEventAsync("WebhookSubscription.Created", new
        {
            item.Id,
            item.Url,
            item.EventTypes,
            item.CreatedAt
        }, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WebhookSubscription>> Put(int id, [FromBody] WebhookSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription is null)
        {
            return BadRequest("Invalid subscription parameters");
        }

        var existing = await _writeContext.WebhookSubscriptions
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (existing is null)
        {
            return NotFound("Subscription not found");
        }

        existing.Url = subscription.Url;
        existing.EventTypes = subscription.EventTypes;
        existing.UpdatedAt = DateTime.Now;

        await _writeContext.SaveChangesAsync(cancellationToken);

        await _webhookDispatcher.EnqueueEventAsync("WebhookSubscription.Updated", new
        {
            existing.Id,
            existing.Url,
            existing.EventTypes,
            existing.UpdatedAt
        }, cancellationToken);

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _writeContext.WebhookSubscriptions
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (existing is null)
        {
            return NotFound("Subscription not found");
        }

        existing.IsActive = false;
        existing.UpdatedAt = DateTime.Now;
        await _writeContext.SaveChangesAsync(cancellationToken);

        await _webhookDispatcher.EnqueueEventAsync("WebhookSubscription.Deleted", new
        {
            existing.Id,
            existing.Url,
            existing.EventTypes,
            existing.UpdatedAt
        }, cancellationToken);

        return Ok();
    }
}
