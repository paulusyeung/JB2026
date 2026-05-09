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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ILegacyIdentityService legacyIdentityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _legacyIdentityService = legacyIdentityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> CreateToken(CancellationToken cancellationToken)
    {
        var (username, password, keepMeSignedIn) = await ResolveCredentialsAsync(cancellationToken);
        return await CreateTokenInternalAsync(username, password, keepMeSignedIn, cancellationToken);
    }

    // Supports legacy clients that call GET /api/{v}/auth/token?username=...&password=...
    // and GET /api/{v}/auth/token with username/password headers.
    [HttpGet("token")]
    public async Task<ActionResult<TokenResponse>> CreateTokenGet([FromQuery] string? username, [FromQuery] string? password)
    {
        var resolvedUsername = string.IsNullOrWhiteSpace(username) ? GetHeaderOrQueryValue("username") : username;
        var resolvedPassword = string.IsNullOrWhiteSpace(password) ? GetHeaderOrQueryValue("password") : password;
        return await CreateTokenInternalAsync(resolvedUsername, resolvedPassword, false, CancellationToken.None);
    }

    [HttpGet("token/{username}/{password}")]
    public async Task<ActionResult<TokenResponse>> CreateTokenLegacyPath(string username, string password)
    {
        return await CreateTokenInternalAsync(username, password, false, CancellationToken.None);
    }

    private async Task<ActionResult<TokenResponse>> CreateTokenInternalAsync(string? username, string? password, bool keepMeSignedIn, CancellationToken cancellationToken)
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

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, keepMeSignedIn);
        _logger.LogInformation("Issued token for username {Username} with role {Role}", user.Username, user.Role);

        string? refreshToken = null;
        if (keepMeSignedIn)
        {
            var refreshTokenExpiryDays = _configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 30;
            refreshToken = await _refreshTokenService.CreateAsync(user.UserId.ToString(), refreshTokenExpiryDays);
            _logger.LogInformation("Issued refresh token for username {Username}", user.Username);
        }

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
            },
            RefreshToken = refreshToken
        });
    }

    private async Task<(string? Username, string? Password, bool KeepMeSignedIn)> ResolveCredentialsAsync(CancellationToken cancellationToken)
    {
        string? username = null;
        string? password = null;
        bool keepMeSignedIn = false;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            username = form[nameof(TokenRequest.Username)].ToString();
            password = form[nameof(TokenRequest.Password)].ToString();
            var keepMeSignedInValue = form[nameof(TokenRequest.KeepMeSignedIn)].ToString();
            if (!string.IsNullOrWhiteSpace(keepMeSignedInValue) && bool.TryParse(keepMeSignedInValue, out var parsed))
            {
                keepMeSignedIn = parsed;
            }
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
                if (request is not null)
                {
                    keepMeSignedIn = request.KeepMeSignedIn;
                }
            }
            catch (JsonException)
            {
                // Fall through to header/query lookup for unsupported or non-JSON payloads.
            }
        }

        username ??= GetHeaderOrQueryValue("username");
        password ??= GetHeaderOrQueryValue("password");

        return (username, password, keepMeSignedIn);
    }

    /// <summary>
    /// Exchanges a refresh token for a new access token and refresh token.
    /// Implements token rotation: the old refresh token is invalidated.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(RefreshTokenRequest.RefreshToken)] = ["Refresh token is required."]
            }));
        }

        // Validate the refresh token
        var userId = await _refreshTokenService.ValidateAsync(request.RefreshToken);
        if (userId is null)
        {
            _logger.LogWarning("Refresh token validation failed for token starting with {TokenPrefix}", 
                request.RefreshToken.Substring(0, Math.Min(10, request.RefreshToken.Length)));
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The supplied refresh token is invalid or expired.",
                Status = StatusCodes.Status401Unauthorized,
                Extensions = new Dictionary<string, object?> { { "error", "invalid_refresh_token" } }
            });
        }

        // Revoke the old refresh token (it was already marked as used by ValidateAsync)
        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        // Look up the user details
        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            _logger.LogError("Invalid user ID format in refresh token: {UserId}", userId);
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid user ID",
                Detail = "The user ID from the refresh token is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var user = _legacyIdentityService.FindByUserId(userIdGuid);
        if (user is null)
        {
            _logger.LogWarning("User not found for ID {UserId}", userId);
            return Unauthorized(new ProblemDetails
            {
                Title = "User not found",
                Detail = "The user associated with this refresh token no longer exists.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Create a new access token
        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, keepMeSignedIn: true);

        // Create a new refresh token
        var refreshTokenExpiryDays = _configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 30;
        var newRefreshToken = await _refreshTokenService.CreateAsync(userId, refreshTokenExpiryDays);

        _logger.LogInformation("Refreshed token for user {UserId}", userId);

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
            },
            RefreshToken = newRefreshToken
        });
    }

    /// <summary>
    /// Revokes a refresh token (logout).
    /// Idempotent: returns 204 for unknown/already-invalid tokens.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(RevokeTokenRequest.RefreshToken)] = ["Refresh token is required."]
            }));
        }

        // Revoke the token (idempotent - does nothing if not found)
        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        _logger.LogInformation("Revoked refresh token");

        // Return 204 No Content for successful revocation (idempotent)
        return NoContent();
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
