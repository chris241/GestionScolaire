using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Settings;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class EducationSettingsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public EducationSettingsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/educationsettings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsSeededSettings_ForAnyAuthenticatedRole()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var settings = await client.GetFromJsonAsync<EducationSettingsDto>("/api/educationsettings");

        Assert.NotNull(settings);
        Assert.Equal("MGA", settings!.Currency);
    }

    [Fact]
    public async Task Director_CanUpdateSettings()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var response = await client.PutAsJsonAsync("/api/educationsettings", new UpdateEducationSettingsRequest(
            "École Test Mise à Jour", "Antananarivo", "MGA", 20));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<EducationSettingsDto>();
        Assert.Equal("École Test Mise à Jour", updated!.SchoolName);
    }

    [Fact]
    public async Task Teacher_CannotUpdateSettings()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PutAsJsonAsync("/api/educationsettings", new UpdateEducationSettingsRequest(
            "École Interdite", null, "MGA", 20));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
