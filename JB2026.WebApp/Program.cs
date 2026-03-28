using JB2026.Infrastructure.Extensions;
using JB2026.WebApp.Middleware;
using JB2026.WebApp.Options;
using JB2026.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddJb2026Foundation();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services
    .AddOptions<UiModernizationOptions>()
    .Bind(builder.Configuration.GetSection(UiModernizationOptions.SectionName));
builder.Services.AddSingleton<IUiFeatureFlagStore, ConfigurationUiFeatureFlagStore>();

var app = builder.Build();
app.UseJb2026Foundation();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<UiSliceRoutingMiddleware>();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

public partial class Program;
