using JB2026.Api.Options;
using JB2026.Api.Services;
using JB2026.EfCore.Data;
using JB2026.Infrastructure.Extensions;
using JB2026.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
FontRegistry.EnsureInitialized();
builder.AddJb2026Foundation();

var isRunningInContainer = string.Equals(
	Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
	"true",
	StringComparison.OrdinalIgnoreCase);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "JB2026.Api",
		Version = "v1",
		Description = "Phase 4 migrated API surface"
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
builder.Services.Configure<JobListOptions>(builder.Configuration.GetSection(JobListOptions.SectionName));
builder.Services.Configure<LegacyFilesOptions>(builder.Configuration.GetSection(LegacyFilesOptions.SectionName));
builder.Services.AddScoped<ILegacyIdentityService, HybridLegacyIdentityService>();
builder.Services.AddSingleton<InMemorySettingsService>();
builder.Services.AddSingleton<IPublicContentService, InMemoryPublicContentService>();
builder.Services.AddSingleton<IHelpContentService, InMemoryHelpContentService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserProfileService, HttpContextCurrentUserProfileService>();

var primaryConnectionString = builder.Configuration.GetConnectionString("Primary");
if (isRunningInContainer && string.IsNullOrWhiteSpace(primaryConnectionString))
{
	throw new InvalidOperationException(
		"Missing required configuration: ConnectionStrings__Primary must be provided when running in a container.");
}

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
	builder.Services.AddScoped<IStockProductPrintComposer, StockProductPrintComposer>();
	builder.Services.AddScoped<IStockProductPdfRenderer, StockProductPdfRenderer>();
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
	builder.Services.AddScoped<ISettingsService, SystemInfoSettingsService>();
}
else
{
	builder.Services.AddSingleton<ISettingsService>(sp => sp.GetRequiredService<InMemorySettingsService>());
	builder.Services.AddSingleton<IQuotationRepository, InMemoryQuotationRepository>();
	builder.Services.AddSingleton<IJobManagementRepository, InMemoryJobManagementRepository>();
}

var app = builder.Build();
app.UseJb2026Foundation();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { Service = "JB2026.Api", Status = "Running" }));
app.MapGet("/healthz", () => Results.Ok(new { Status = "Healthy" }));

app.Run();

public partial class Program;

