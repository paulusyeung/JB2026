using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JB2026.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/settings/rbac")]
public sealed class RbacController : ControllerBase
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<RbacController> _logger;

    public RbacController(IRbacService rbacService, ILogger<RbacController> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpGet("effective")]
    [ProducesResponseType(typeof(RbacValuesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RbacValuesResponse>> GetEffectiveRbac(CancellationToken cancellationToken)
    {
        var snapshot = await _rbacService.GetEffectiveRbacAsync(cancellationToken);
        return Ok(new RbacValuesResponse { Values = snapshot.Values });
    }

    [HttpGet("group")]
    [ProducesResponseType(typeof(RbacValuesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RbacValuesResponse>> GetGroupRbac(
        [FromQuery] string role,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(role)] = ["Role is required to read group RBAC."]
            }));
        }

        var snapshot = await _rbacService.GetGroupRbacAsync(role, cancellationToken);
        return Ok(new RbacValuesResponse { Values = snapshot.Values });
    }

    [HttpPut("group")]
    [ProducesResponseType(typeof(RbacValuesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RbacValuesResponse>> SaveGroupRbac(
        [FromQuery] string role,
        [FromBody] SaveRbacRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(role)] = ["Role is required to save group RBAC."]
            }));
        }

        if (request.Values is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Values)] = ["RBAC values are required."]
            }));
        }

        await _rbacService.SaveGroupRbacAsync(role, request.Values, cancellationToken);
        _logger.LogInformation("Saved group RBAC for role {Role} with {Count} entries", role, request.Values.Count);

        return Ok(new RbacValuesResponse { Values = request.Values });
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(RbacValuesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RbacValuesResponse>> GetUserRbac(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _rbacService.GetUserRbacAsync(userId, cancellationToken);
            return Ok(new RbacValuesResponse { Values = snapshot.Values });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = $"User {userId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPut("user/{userId:guid}")]
    [ProducesResponseType(typeof(RbacValuesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RbacValuesResponse>> SaveUserRbac(
        Guid userId,
        [FromBody] SaveRbacRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Values is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Values)] = ["RBAC values are required."]
            }));
        }

        try
        {
            await _rbacService.SaveUserRbacAsync(userId, request.Values, cancellationToken);
            _logger.LogInformation("Saved user RBAC for user {UserId} with {Count} entries", userId, request.Values.Count);

            return Ok(new RbacValuesResponse { Values = request.Values });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = $"User {userId} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}
