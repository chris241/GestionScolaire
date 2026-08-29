using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.GradingScales;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class GradingScalesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public GradingScalesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededDefaultScale_WithFiveIntervals()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var scales = await client.GetFromJsonAsync<List<GradingScaleDto>>("/api/gradingscales");

        Assert.NotNull(scales);
        var defaultScale = Assert.Single(scales!, s => s.IsDefault);
        Assert.Equal(5, defaultScale.Intervals.Count);
        Assert.Contains(defaultScale.Intervals, i => i.Grade == "A");
    }

    [Fact]
    public async Task Director_CanCreateScale_AndAddInterval()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/gradingscales", new CreateGradingScaleRequest("Barème Test", false));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<GradingScaleDto>();

        var intervalResponse = await client.PostAsJsonAsync($"/api/gradingscales/{created!.Id}/intervals",
            new CreateGradingScaleIntervalRequest("A+", 18, 20));
        intervalResponse.EnsureSuccessStatusCode();

        var scales = await client.GetFromJsonAsync<List<GradingScaleDto>>("/api/gradingscales");
        var refreshed = scales!.Single(s => s.Id == created.Id);
        Assert.Single(refreshed.Intervals);
    }

    [Fact]
    public async Task Teacher_CannotCreateScale()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/gradingscales", new CreateGradingScaleRequest("Interdit", false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
