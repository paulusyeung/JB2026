using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.Rest.Helpers;
using JB2026.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.AddJb2026Foundation();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "JB2026.Rest",
		Version = "v1",
		Description = "Phase 4 REST host contract"
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Provide a valid JWT access token."
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.Configure<LegacyIdentityOptions>(builder.Configuration.GetSection(LegacyIdentityOptions.SectionName));
builder.Services.AddSingleton<ILegacyIdentityService, ConfiguredLegacyIdentityService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserProfileService, HttpContextCurrentUserProfileService>();
builder.Services.AddScoped<IFcmEventHelperService, FcmEventHelperService>();
builder.Services.AddHttpClient(nameof(WebhookDispatcherService), client =>
{
	client.Timeout = TimeSpan.FromSeconds(5);
});

var primaryConnectionString = builder.Configuration.GetConnectionString("Primary");
if (!string.IsNullOrWhiteSpace(primaryConnectionString))
{
	builder.Services.AddDbContext<JB5LegacyReadContext>(options =>
		options
			.UseSqlServer(primaryConnectionString)
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

	builder.Services.AddDbContext<JB5LegacyWriteContext>(options =>
		options.UseSqlServer(primaryConnectionString));

	builder.Services.AddScoped<IQuotationRepository, EfQuotationRepository>();
	builder.Services.AddScoped<IJobManagementRepository, EfJobManagementRepository>();
	builder.Services.AddScoped<IJobAttachmentStoredProcedureGateway, JobAttachmentStoredProcedureGateway>();
	builder.Services.AddScoped<IJobScheduleStoredProcedureGateway, JobScheduleStoredProcedureGateway>();
	builder.Services.AddScoped<IJobOrderStoredProcedureGateway, JobOrderStoredProcedureGateway>();
	builder.Services.AddScoped<IJobPackingOnAirStoredProcedureGateway, JobPackingOnAirStoredProcedureGateway>();
	builder.Services.AddScoped<IProductStoredProcedureGateway, ProductStoredProcedureGateway>();
	builder.Services.AddScoped<ISupplierStoredProcedureGateway, SupplierStoredProcedureGateway>();
	builder.Services.AddScoped<IProductAttachmentStoredProcedureGateway, ProductAttachmentStoredProcedureGateway>();
	builder.Services.AddScoped<IStockInOutStoredProcedureGateway, StockInOutStoredProcedureGateway>();
	builder.Services.AddScoped<ICustomerStoredProcedureGateway, CustomerStoredProcedureGateway>();
	builder.Services.AddScoped<IInvoiceHeaderStoredProcedureGateway, InvoiceHeaderStoredProcedureGateway>();
	builder.Services.AddScoped<IInvoiceItemStoredProcedureGateway, InvoiceItemStoredProcedureGateway>();
	builder.Services.AddScoped<IInvoiceSubItemStoredProcedureGateway, InvoiceSubItemStoredProcedureGateway>();
	builder.Services.AddScoped<IJobWorkflowStoredProcedureGateway, JobWorkflowStoredProcedureGateway>();
	builder.Services.AddScoped<IJobWorkflowFormStoredProcedureGateway, JobWorkflowFormStoredProcedureGateway>();
	builder.Services.AddScoped<IZCategoryStoredProcedureGateway, ZCategoryStoredProcedureGateway>();
	builder.Services.AddScoped<IZFormStoredProcedureGateway, ZFormStoredProcedureGateway>();
	builder.Services.AddScoped<IZWorkflowStoredProcedureGateway, ZWorkflowStoredProcedureGateway>();
	builder.Services.AddScoped<IZWorkflowFormStoredProcedureGateway, ZWorkflowFormStoredProcedureGateway>();
	builder.Services.AddScoped<IZOrderTypeWorkflowStoredProcedureGateway, ZOrderTypeWorkflowStoredProcedureGateway>();
	builder.Services.AddScoped<ISmlRtfHeaderStoredProcedureGateway, SmlRtfHeaderStoredProcedureGateway>();
	builder.Services.AddScoped<ISmlRtfItemStoredProcedureGateway, SmlRtfItemStoredProcedureGateway>();
	builder.Services.AddScoped<ISmlRtfSubItemStoredProcedureGateway, SmlRtfSubItemStoredProcedureGateway>();
	builder.Services.AddScoped<ISmlRtfExtractToDNStoredProcedureGateway, SmlRtfExtractToDNStoredProcedureGateway>();
	builder.Services.AddScoped<IUserInfoStoredProcedureGateway, UserInfoStoredProcedureGateway>();
	builder.Services.AddScoped<ISystemInfoStoredProcedureGateway, SystemInfoStoredProcedureGateway>();

	builder.Services.AddHangfire(configuration => configuration
		.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
		.UseSimpleAssemblyNameTypeSerializer()
		.UseRecommendedSerializerSettings()
		.UseSqlServerStorage(primaryConnectionString, new SqlServerStorageOptions
		{
			CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
			SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
			QueuePollInterval = TimeSpan.Zero,
			UseRecommendedIsolationLevel = true
		}));

	builder.Services.AddHangfireServer();
	builder.Services.AddScoped<IWebhookDispatcherService, WebhookDispatcherService>();
}
else
{
	builder.Services.AddSingleton<IQuotationRepository, InMemoryQuotationRepository>();
	builder.Services.AddSingleton<IJobManagementRepository, InMemoryJobManagementRepository>();
	builder.Services.AddScoped<IWebhookDispatcherService, WebhookDispatcherService>();
}

var app = builder.Build();
app.UseJb2026Foundation();

app.UseSwagger();
app.UseSwaggerUI();

if (!string.IsNullOrWhiteSpace(primaryConnectionString))
{
	app.UseHangfireDashboard("/hangfire", new DashboardOptions
	{
		Authorization = new[] { new LocalRequestsOnlyDashboardAuthorizationFilter() }
	});
}

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { Service = "JB2026.Rest", Status = "Running" }));

app.Run();

public partial class Program;

internal sealed class LocalRequestsOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context)
	{
		var httpContext = context.GetHttpContext();
		var remoteIp = httpContext.Connection.RemoteIpAddress;
		var localIp = httpContext.Connection.LocalIpAddress;

		if (remoteIp is null)
		{
			return false;
		}

		if (System.Net.IPAddress.IsLoopback(remoteIp))
		{
			return true;
		}

		return localIp is not null && remoteIp.Equals(localIp);
	}
}
