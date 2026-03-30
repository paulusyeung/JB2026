using JB2026.Api.Controllers;
using JB2026.Api.Models;
using JB2026.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace JB2026.Api.ParityTests;

public sealed class SettingsControllerTests
{
    [Fact]
    public void Get_ReturnsCurrentSettings()
    {
        var service = new InMemorySettingsService();
        var controller = CreateController(service);

        var result = controller.Get();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var settings = Assert.IsType<SettingsResponse>(ok.Value);

        Assert.Equal("JB2026 Printing", settings.CompanyName);
    }

    [Fact]
    public void Update_ValidRequest_ReturnsUpdatedSettings()
    {
        var service = new InMemorySettingsService();
        var controller = CreateController(service);

        var result = controller.Update(new UpdateSettingsRequest
        {
            CompanyName = "Acme Print",
            TimeZone = "Asia/Singapore",
            CurrencyCode = "sgd",
            EnableLegacyFallback = false,
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var settings = Assert.IsType<SettingsResponse>(ok.Value);

        Assert.Equal("Acme Print", settings.CompanyName);
        Assert.Equal("Asia/Singapore", settings.TimeZone);
        Assert.Equal("SGD", settings.CurrencyCode);
        Assert.False(settings.EnableLegacyFallback);
    }

    private static SettingsController CreateController(ISettingsService service)
    {
        var controller = new SettingsController(service, NullLogger<SettingsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }
}