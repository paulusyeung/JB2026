using JB2026.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.AddJb2026Foundation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "JB2026.Rest",
		Version = "v1",
		Description = "Phase 4 REST host contract"
	});
});

var app = builder.Build();
app.UseJb2026Foundation();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { Service = "JB2026.Rest", Status = "Running" }));

app.Run();
