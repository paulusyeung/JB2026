using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using JB2026.EfCore.Data;
using JB2026.Rest.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JB2026.Rest.Tests;

/// <summary>
/// Shared WebApplicationFactory for JB2026.Rest compatibility controller integration tests.
/// Replaces the SQL Server connection with an InMemory EF Core database and provides
/// helpers for authenticated HTTP clients and JWT token generation.
/// </summary>
public sealed class RestTestFixture : WebApplicationFactory<TokenCompatibilityController>
{
    internal const string TestJwtKey = "rest-test-signing-key-must-be-at-least-32-chars!";
    internal const string TestJwtIssuer = "jb2026-api";
    internal const string TestJwtAudience = "jb2026-clients";

    internal static readonly Guid TestAdminUserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    // Each fixture instance gets its own isolated InMemory database.
    private readonly string _dbName = $"RestTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // AddJb2026Foundation loads environment variables last; use that precedence
        // to force Program.cs into the no-SQL branch during tests.
        Environment.SetEnvironmentVariable("ConnectionStrings__Primary", " ");
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        Environment.SetEnvironmentVariable("LegacyIdentity__Users__0__UserId", TestAdminUserId.ToString());
        Environment.SetEnvironmentVariable("LegacyIdentity__Users__0__Username", "admin");
        Environment.SetEnvironmentVariable("LegacyIdentity__Users__0__Password", "adminpass");
        Environment.SetEnvironmentVariable("LegacyIdentity__Users__0__DisplayName", "Test Admin");
        Environment.SetEnvironmentVariable("LegacyIdentity__Users__0__Role", "Admin");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["LegacyIdentity:Users:0:UserId"] = TestAdminUserId.ToString(),
                ["LegacyIdentity:Users:0:Username"] = "admin",
                ["LegacyIdentity:Users:0:Password"] = "adminpass",
                ["LegacyIdentity:Users:0:DisplayName"] = "Test Admin",
                ["LegacyIdentity:Users:0:Role"] = "Admin",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Add InMemory EF Core contexts so compatibility controllers that inject them
            // can be activated without a real SQL Server connection.
            // Both contexts share the same InMemory store so writes via JB5LegacyWriteContext
            // are immediately visible to JB5LegacyReadContext.
            services.AddDbContext<JB5LegacyReadContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            services.AddDbContext<JB5LegacyWriteContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Creates an HttpClient pre-configured with a valid Bearer token.</summary>
    internal HttpClient CreateAuthenticatedClient(string role = "Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(TestAdminUserId, role));
        return client;
    }

    /// <summary>Generates a signed JWT token using the test signing key.</summary>
    internal static string GenerateToken(Guid userId, string role = "Admin", string username = "admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("legacy_username", username),
            new Claim("display_name", username),
        };
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Seeds data into the shared InMemory database within a scoped service context.</summary>
    internal async Task SeedAsync(Action<JB5LegacyWriteContext> seed)
    {
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<JB5LegacyWriteContext>();
        seed(ctx);
        await ctx.SaveChangesAsync();
    }

    internal async Task<T> ReadAsync<T>(Func<JB5LegacyReadContext, Task<T>> read)
    {
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<JB5LegacyReadContext>();
        return await read(ctx);
    }
}
