using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.FeeCategories;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class FeeCategoriesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public FeeCategoriesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededCategories()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");

        Assert.NotNull(categories);
        Assert.Contains(categories!, c => c.Name == "Scolarité");
        Assert.Contains(categories!, c => c.Name == "Cantine");
        Assert.Contains(categories!, c => c.Name == "Transport");
    }

    [Fact]
    public async Task Director_CanCreateAndDeleteCategory()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/feecategories", new CreateFeeCategoryRequest("Fournitures", null, false));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<FeeCategoryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/feecategories/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotAccessFeeCategories()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/feecategories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
