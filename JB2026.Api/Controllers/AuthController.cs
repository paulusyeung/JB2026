using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JB2026.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v2/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ILegacyIdentityService _legacyIdentityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ILegacyIdentityService legacyIdentityService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _legacyIdentityService = legacyIdentityService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> CreateToken(CancellationToken cancellationToken)
    {
        var (username, password) = await ResolveCredentialsAsync(cancellationToken);
        return CreateTokenInternal(username, password);
    }

    // Supports legacy clients that call GET /api/{v}/auth/token?username=...&password=...
    // and GET /api/{v}/auth/token with username/password headers.
    [HttpGet("token")]
    public ActionResult<TokenResponse> CreateTokenGet([FromQuery] string? username, [FromQuery] string? password)
    {
        var resolvedUsername = string.IsNullOrWhiteSpace(username) ? GetHeaderOrQueryValue("username") : username;
        var resolvedPassword = string.IsNullOrWhiteSpace(password) ? GetHeaderOrQueryValue("password") : password;
        return CreateTokenInternal(resolvedUsername, resolvedPassword);
    }

    [HttpGet("token/{username}/{password}")]
    public ActionResult<TokenResponse> CreateTokenLegacyPath(string username, string password)
    {
        return CreateTokenInternal(username, password);
    }

    private ActionResult<TokenResponse> CreateTokenInternal(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(TokenRequest.Username)] = ["Username is required."],
                [nameof(TokenRequest.Password)] = ["Password is required."]
            }));
        }

        var user = _legacyIdentityService.ValidateCredentials(username, password);
        if (user is null)
        {
            _logger.LogWarning("Authentication failed for username {Username}", username);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "The supplied username or password is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);
        _logger.LogInformation("Issued token for username {Username} with role {Role}", user.Username, user.Role);

        return Ok(new TokenResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer",
            User = new UserProfileResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role
            }
        });
    }

    private async Task<(string? Username, string? Password)> ResolveCredentialsAsync(CancellationToken cancellationToken)
    {
        string? username = null;
        string? password = null;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            username = form[nameof(TokenRequest.Username)].ToString();
            password = form[nameof(TokenRequest.Password)].ToString();
        }
        else if (Request.ContentLength is > 0)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<TokenRequest>(
                    Request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                username = request?.Username;
                password = request?.Password;
            }
            catch (JsonException)
            {
                // Fall through to header/query lookup for unsupported or non-JSON payloads.
            }
        }

        username ??= GetHeaderOrQueryValue("username");
        password ??= GetHeaderOrQueryValue("password");

        return (username, password);
    }

    private string? GetHeaderOrQueryValue(string key)
    {
        var fromQuery = Request.Query[key].ToString();
        if (!string.IsNullOrWhiteSpace(fromQuery))
        {
            return fromQuery;
        }

        var fromHeader = Request.Headers[key].ToString();
        if (!string.IsNullOrWhiteSpace(fromHeader))
        {
            return fromHeader;
        }

        return null;
    }
}
