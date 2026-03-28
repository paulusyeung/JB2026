using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace JB2026.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring JB2026 middleware pipeline.
/// Includes CORS, authentication, and error handling.
/// </summary>
public static class Jb2026ApplicationBuilderExtensions
{
    public static WebApplication UseJb2026Foundation(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        
        // Error handling middleware
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // CORS middleware (Task 2.3 - must come before authentication)
        var corsPolicy = app.Environment.IsDevelopment() ? "DevelopmentPolicy" : "ProductionPolicy";
        app.UseCors(corsPolicy);

        // Authentication and Authorization middleware (Task 2.4)
        app.UseAuthentication();
        app.UseAuthorization();

        // Health check endpoints
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        return app;
    }
}
