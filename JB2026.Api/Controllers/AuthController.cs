using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JB2026.Api.Models;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    private readonly ITwoFactorService _twoFactorService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    private static readonly ConcurrentDictionary<string, (int Failures, DateTime? LockedUntil)> _twoFactorAttempts = new();

    public AuthController(
        ILegacyIdentityService legacyIdentityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ITwoFactorService twoFactorService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _legacyIdentityService = legacyIdentityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _twoFactorService = twoFactorService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> CreateToken(CancellationToken cancellationToken)
    {
        var (username, password, keepMeSignedIn) = await ResolveCredentialsAsync(cancellationToken);
        return await CreateTokenInternalAsync(username, password, keepMeSignedIn, cancellationToken);
    }

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

        // Check if 2FA is enabled for this user
        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(user.UserId);
        if (twoFactorEnabled)
        {
            // Issue a temporary 2FA token instead of a full access token
            var twoFactorToken = CreateTwoFactorToken(user);
            _logger.LogInformation("Issued 2FA token for username {Username}", user.Username);

            return Ok(new TokenResponse
            {
                AccessToken = string.Empty,
                ExpiresAtUtc = DateTime.UtcNow,
                TokenType = "Bearer",
                User = new UserProfileResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    TwoFactorEnabled = true
                },
                Requires2fa = true,
                TwoFactorToken = twoFactorToken
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
                Role = user.Role,
                TwoFactorEnabled = false
            },
            RefreshToken = refreshToken
        });
    }

    [HttpPost("2fa/verify")]
    public async Task<ActionResult<TokenResponse>> VerifyTwoFactor([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.TwoFactorToken) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["TwoFactorToken"] = ["Two-factor token is required."],
                ["Code"] = ["Verification code is required."]
            }));
        }

        // Validate the temporary 2FA token
        var userId = ValidateTwoFactorToken(request.TwoFactorToken);
        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid or expired token",
                Detail = "The two-factor token is invalid or has expired. Please log in again.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var userIdGuid = Guid.Parse(userId);

        // Check rate limiting
        if (IsTwoFactorLocked(userId))
        {
            _logger.LogWarning("2FA rate limit exceeded for user {UserId}", userId);
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail = "Too many failed attempts. Please try again later.",
                Status = 429
            });
        }

        // Check if user still has 2FA enabled
        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid);
        if (!twoFactorEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "2FA not enabled",
                Detail = "Two-factor authentication is not enabled for this account.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Try TOTP code first, then recovery code
        var validCode = false;
        var user = _legacyIdentityService.FindByUserId(userIdGuid);
        if (user is not null)
        {
            // Get the encrypted secret from the database
            var userInfo = await GetUserInfoMetadataAsync(userIdGuid);
            var encryptedSecret = MetadataXmlHelper.ExtractTwoFactorSecret(userInfo);
            if (!string.IsNullOrEmpty(encryptedSecret))
            {
                var secret = _twoFactorService.DecryptSecret(encryptedSecret);
                validCode = _twoFactorService.ValidateCode(secret, request.Code);
            }

            if (!validCode)
            {
                // Try recovery code
                var hashedRecoveryCodes = MetadataXmlHelper.ExtractTwoFactorRecoveryCodes(userInfo);
                var (success, updatedCodes) = _twoFactorService.VerifyRecoveryCode(hashedRecoveryCodes, request.Code);
                validCode = success;

                if (success && !string.IsNullOrEmpty(updatedCodes))
                {
                    // Save the updated recovery codes (with used code removed)
                    await _legacyIdentityService.EnableTwoFactorAsync(userIdGuid, encryptedSecret, updatedCodes);
                }
            }
        }

        if (!validCode)
        {
            RecordTwoFactorFailure(userId);
            _logger.LogWarning("Invalid 2FA code for user {UserId}", userId);
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid code",
                Detail = "The verification code is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Reset failure count on success
        ResetTwoFactorFailures(userId);

        // Issue full access token
        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user!, keepMeSignedIn: false);
        _logger.LogInformation("2FA verified for user {UserId}, issued token", userId);

        return Ok(new TokenResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            TokenType = "Bearer",
            User = new UserProfileResponse
            {
                UserId = user!.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
                TwoFactorEnabled = true
            }
        });
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor(
        [FromBody] TwoFactorSetupRequest? request,
        [FromServices] JB5LegacyWriteContext writeContext)
    {
        var currentUserId = GetUserIdFromClaims();
        if (currentUserId is null)
            return Unauthorized();

        if (!TryResolveTargetUserId(request?.UserId, out var userIdGuid))
            return Forbid();

        // Check if 2FA is already enabled
        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid);
        if (twoFactorEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "2FA already enabled",
                Detail = "Two-factor authentication is already enabled for this account.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var secret = _twoFactorService.GenerateSecret();
        var encryptedSecret = _twoFactorService.EncryptSecret(secret);
        var provisioningUri = _twoFactorService.GetProvisioningUri(userIdGuid.ToString(), secret);

        // Save the encrypted secret to database (Enabled=false until confirmed)
        var userInfo = await writeContext.UserInfos.FirstOrDefaultAsync(u => u.UserId == userIdGuid);
        if (userInfo is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "User not found",
                Detail = "User record not found in database.",
                Status = StatusCodes.Status404NotFound
            });
        }

        userInfo.MetadataXml = MetadataXmlHelper.SetTwoFactorInMetadata(userInfo.MetadataXml, false, encryptedSecret, string.Empty);
        await writeContext.SaveChangesAsync();

        return Ok(new TwoFactorSetupResponse
        {
            Secret = encryptedSecret,
            ProvisioningUri = provisioningUri
        });
    }

    [Authorize]
    [HttpPost("2fa/confirm")]
    public async Task<ActionResult<TwoFactorConfirmResponse>> ConfirmTwoFactor([FromBody] TwoFactorConfirmRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Code"] = ["Verification code is required."]
            }));
        }

        if (!TryResolveTargetUserId(request.UserId, out var userIdGuid))
            return Forbid();

        // Check if 2FA is already enabled
        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid);
        if (twoFactorEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "2FA already enabled",
                Detail = "Two-factor authentication is already enabled for this account.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userInfo = await GetUserInfoMetadataAsync(userIdGuid);
        var encryptedSecret = MetadataXmlHelper.ExtractTwoFactorSecret(userInfo);

        if (string.IsNullOrEmpty(encryptedSecret))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Setup required",
                Detail = "Please call /2fa/setup first.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var secret = _twoFactorService.DecryptSecret(encryptedSecret);
        var validCode = _twoFactorService.ValidateCode(secret, request.Code);

        if (!validCode)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid code",
                Detail = "The verification code is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();
        var hashedRecoveryCodes = _twoFactorService.HashRecoveryCodes(recoveryCodes);

        await _legacyIdentityService.EnableTwoFactorAsync(userIdGuid, encryptedSecret, hashedRecoveryCodes);

        _logger.LogInformation("2FA enabled for user {UserId}", userIdGuid);

        return Ok(new TwoFactorConfirmResponse
        {
            RecoveryCodes = recoveryCodes
        });
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["Password"] = ["Password is required."],
                ["Code"] = ["Verification code is required."]
            }));
        }

        if (!TryResolveTargetUserId(request.UserId, out var userIdGuid))
            return Forbid();

        var user = _legacyIdentityService.FindByUserId(userIdGuid);
        if (user is null || user.Password != request.Password)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "The password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (IsTwoFactorLocked(userIdGuid.ToString()))
        {
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail = "Too many failed attempts. Please try again later.",
                Status = 429
            });
        }

        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid);
        if (!twoFactorEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "2FA not enabled",
                Detail = "Two-factor authentication is not enabled for this account.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userInfo = await GetUserInfoMetadataAsync(userIdGuid);
        var encryptedSecret = MetadataXmlHelper.ExtractTwoFactorSecret(userInfo);
        var validCode = false;

        if (!string.IsNullOrEmpty(encryptedSecret))
        {
            var secret = _twoFactorService.DecryptSecret(encryptedSecret);
            validCode = _twoFactorService.ValidateCode(secret, request.Code);
        }

        if (!validCode)
        {
            var hashedRecoveryCodes = MetadataXmlHelper.ExtractTwoFactorRecoveryCodes(userInfo);
            var (success, _) = _twoFactorService.VerifyRecoveryCode(hashedRecoveryCodes, request.Code);
            validCode = success;
        }

        if (!validCode)
        {
            RecordTwoFactorFailure(userIdGuid.ToString());
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid code",
                Detail = "The verification code is invalid.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        await _legacyIdentityService.DisableTwoFactorAsync(userIdGuid);
        ResetTwoFactorFailures(userIdGuid.ToString());

        _logger.LogInformation("2FA disabled for user {UserId}", userIdGuid);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("2fa")]
    public async Task<IActionResult> AdminDisableTwoFactor([FromBody] TwoFactorAdminDisableRequest request)
    {
        if (request is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["UserId"] = ["User ID is required."]
            }));
        }

        var twoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(request.UserId);
        if (!twoFactorEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "2FA not enabled",
                Detail = "Two-factor authentication is not enabled for this user.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await _legacyIdentityService.DisableTwoFactorAsync(request.UserId);

        _logger.LogInformation("Admin disabled 2FA for user {UserId}", request.UserId);

        return NoContent();
    }

    [Authorize]
    [HttpGet("2fa/status")]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetTwoFactorStatus([FromQuery] Guid? userId)
    {
        if (!TryResolveTargetUserId(userId, out var userIdGuid))
            return Forbid();

        var enabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid);

        return Ok(new TwoFactorStatusResponse
        {
            Enabled = enabled
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

        var userId = await _refreshTokenService.ValidateAndConsumeAsync(request.RefreshToken);
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

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, keepMeSignedIn: true);

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
                Role = user.Role,
                TwoFactorEnabled = await _legacyIdentityService.GetTwoFactorStatusAsync(userIdGuid)
            },
            RefreshToken = newRefreshToken
        });
    }

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

        await _refreshTokenService.RevokeAsync(request.RefreshToken);

        _logger.LogInformation("Revoked refresh token");

        return NoContent();
    }

    private string CreateTwoFactorToken(LegacyIdentityUser user)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "jb2026-api";
        var audience = _configuration["Jwt:Audience"] ?? "jb2026-clients";
        var signingKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("purpose", "2fa")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string? ValidateTwoFactorToken(string token)
    {
        var signingKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(signingKey))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "jb2026-api",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "jb2026-clients",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            // Verify purpose claim
            var purpose = principal.FindFirst("purpose")?.Value;
            if (purpose != "2fa")
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private string? GetUserIdFromClaims()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private bool TryResolveTargetUserId(Guid? requestedUserId, out Guid targetUserId)
    {
        targetUserId = Guid.Empty;

        var currentUserId = GetUserIdFromClaims();
        if (currentUserId is null)
            return false;

        var currentUserGuid = Guid.Parse(currentUserId);

        if (!requestedUserId.HasValue || requestedUserId.Value == currentUserGuid)
        {
            targetUserId = currentUserGuid;
            return true;
        }

        if (!User.IsInRole("Admin"))
            return false;

        targetUserId = requestedUserId.Value;
        return true;
    }

    private bool IsTwoFactorLocked(string userId)
    {
        if (!_twoFactorAttempts.TryGetValue(userId, out var attempt))
            return false;

        if (attempt.LockedUntil.HasValue && attempt.LockedUntil > DateTime.UtcNow)
            return true;

        if (attempt.LockedUntil.HasValue && attempt.LockedUntil <= DateTime.UtcNow)
        {
            _twoFactorAttempts.TryUpdate(userId, (0, null), attempt);
            return false;
        }

        return false;
    }

    private void RecordTwoFactorFailure(string userId)
    {
        _twoFactorAttempts.AddOrUpdate(userId,
            (1, null),
            (_, existing) =>
            {
                var failures = existing.Failures + 1;
                if (failures >= 5)
                {
                    return (failures, DateTime.UtcNow.AddMinutes(5));
                }
                return (failures, null);
            });
    }

    private void ResetTwoFactorFailures(string userId)
    {
        _twoFactorAttempts.TryRemove(userId, out _);
    }

    private async Task<string?> GetUserInfoMetadataAsync(Guid userId)
    {
        return await _legacyIdentityService.GetUserInfoMetadataAsync(userId);
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
