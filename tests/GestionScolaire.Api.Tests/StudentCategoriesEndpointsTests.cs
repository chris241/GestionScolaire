using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.StudentCategories;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentCategoriesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentCategoriesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/studentcategories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsSeededCategories_ForAnyAuthenticatedRole()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var categories = await client.GetFromJsonAsync<List<StudentCategoryDto>>("/api/studentcategories");

        Assert.NotNull(categories);
        Assert.Contains(categories!, c => c.Name == "Standard");
        Assert.Contains(categories!, c => c.Name == "Boursier");
    }

    [Fact]
    public async Task Director_CanCreateAndDeleteCategory()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/studentcategories", new CreateStudentCategoryRequest("Test Catégorie", null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentCategoryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/studentcategories/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateCategory()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/studentcategories", new CreateStudentCategoryRequest("Interdit", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
