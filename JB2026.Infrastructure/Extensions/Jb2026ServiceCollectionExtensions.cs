using System.Reflection;
using JB2026.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;

namespace JB2026.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring JB2026 foundation services.
/// Includes CORS, authentication, observability, and health checks.
/// </summary>
public static class Jb2026ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddJb2026Foundation(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        if (builder.Environment.IsDevelopment())
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
            {
                builder.Configuration.AddUserSecrets(entryAssembly, optional: true);
            }
        }

        builder.Services
            .AddOptions<Jb2026EnvironmentOptions>()
            .Bind(builder.Configuration.GetSection(Jb2026EnvironmentOptions.SectionName));

        builder.Services
            .AddOptions<Jb2026ObservabilityOptions>()
            .Bind(builder.Configuration.GetSection(Jb2026ObservabilityOptions.SectionName));

        builder.Services.AddProblemDetails();
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        var serviceName = builder.Configuration[$"{Jb2026ObservabilityOptions.SectionName}:ServiceName"]
            ?? builder.Environment.ApplicationName;
        var otlpEndpoint = builder.Configuration[$"{Jb2026ObservabilityOptions.SectionName}:OtlpEndpoint"];

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            });

        // Add CORS support (Task 2.3)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DevelopmentPolicy", policyBuilder =>
            {
                policyBuilder
                    .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8080")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            options.AddPolicy("ProductionPolicy", policyBuilder =>
            {
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                    ?? Array.Empty<string>();
                
                if (allowedOrigins.Any())
                {
                    policyBuilder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        // Add JWT Bearer authentication (Task 2.4 - Phase 1 approved architecture)
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"] ?? "your-secret-key-this-should-be-in-user-secrets";
        var jwtIssuer = jwtSettings["Issuer"] ?? "jb2026-api";
        var jwtAudience = jwtSettings["Audience"] ?? "jb2026-clients";

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(10),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuerSigningKey = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (builder.Environment.IsDevelopment())
                        {
                            Serilog.Log.Warning("JWT validation failed: {Message}", context.Exception.Message);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        return builder;
    }
}
