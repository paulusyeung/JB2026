using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JB2026.Rest.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class TokenCompatibilityController : ControllerBase
{
    private readonly ILegacyIdentityService _legacyIdentityService;
    private readonly IConfiguration _configuration;

    public TokenCompatibilityController(ILegacyIdentityService legacyIdentityService, IConfiguration configuration)
    {
        _legacyIdentityService = legacyIdentityService;
        _configuration = configuration;
    }

    [HttpGet("api/Token")]
    public ActionResult<string> Get()
    {
        var (username, password) = ResolveHeaderCredentials();
        return IssueToken(username, password);
    }

    [HttpGet("api/Token/{username}/{password}")]
    public ActionResult<string> Get(string username, string password)
    {
        return IssueToken(username, password);
    }

    [HttpGet("api/Token/{username}/{password}/{expiry}")]
    public ActionResult<string> Get(string username, string password, string expiry)
    {
        var overrideExpiry = ParseExpiryMinutes(expiry);
        return IssueToken(username, password, overrideExpiryMinutes: overrideExpiry);
    }

    [HttpGet("api/Token/Staff")]
    public ActionResult<string> GetStaff()
    {
        var (username, password) = ResolveHeaderCredentials();
        return IssueToken(username, password, rolePredicate: IsStaffRole);
    }

    [HttpGet("api/Token/Staff/{username}/{password}")]
    public ActionResult<string> GetStaff(string username, string password)
    {
        return IssueToken(username, password, rolePredicate: IsStaffRole);
    }

    [HttpGet("api/Token/Staff/{username}/{password}/{expiry}")]
    public ActionResult<string> GetStaff(string username, string password, string expiry)
    {
        var overrideExpiry = ParseExpiryMinutes(expiry);
        return IssueToken(username, password, rolePredicate: IsStaffRole, overrideExpiryMinutes: overrideExpiry);
    }

    [HttpGet("api/Token/Client")]
    public ActionResult<string> GetClient()
    {
        var (username, password) = ResolveHeaderCredentials();
        return IssueToken(username, password, rolePredicate: IsClientRole);
    }

    [HttpGet("api/Token/Client/{username}/{password}")]
    public ActionResult<string> GetClient(string username, string password)
    {
        return IssueToken(username, password, rolePredicate: IsClientRole);
    }

    private ActionResult<string> IssueToken(
        string? username,
        string? password,
        Func<string, bool>? rolePredicate = null,
        int? overrideExpiryMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Unauthorized();
        }

        var user = _legacyIdentityService.ValidateCredentials(username, password);
        if (user is null)
        {
            return Unauthorized();
        }

        if (rolePredicate is not null && !rolePredicate(user.Role))
        {
            return Unauthorized();
        }

        return CreateToken(user, overrideExpiryMinutes);
    }

    private string CreateToken(LegacyIdentityUser user, int? overrideExpiryMinutes)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "jb2026-api";
        var audience = _configuration["Jwt:Audience"] ?? "jb2026-clients";
        var signingKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is missing.");
        var expiryMinutes = overrideExpiryMinutes ?? _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            // Legacy JB5.REST used ClaimTypes.Name to carry the user SID.
            new(ClaimTypes.Name, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new("legacy_username", user.Username),
            new("display_name", user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (string? Username, string? Password) ResolveHeaderCredentials()
    {
        var username = Request.Headers["username"].ToString();
        var password = Request.Headers["password"].ToString();

        return (
            string.IsNullOrWhiteSpace(username) ? null : username,
            string.IsNullOrWhiteSpace(password) ? null : password);
    }

    private static int? ParseExpiryMinutes(string expiry)
    {
        if (!DateTime.TryParseExact(
                expiry,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var expiryDate))
        {
            return null;
        }

        var delta = expiryDate - DateTime.Now;
        if (delta.TotalMinutes <= 0)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Floor(delta.TotalMinutes));
    }

    private static bool IsStaffRole(string role)
    {
        return role.Equals("staff", StringComparison.OrdinalIgnoreCase)
            || role.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClientRole(string role)
    {
        return role.Equals("customer", StringComparison.OrdinalIgnoreCase)
            || role.Equals("client", StringComparison.OrdinalIgnoreCase);
    }
}
