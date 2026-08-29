using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicTerms;
using GestionScolaire.Application.DTOs.AcademicYears;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AcademicTermsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AcademicTermsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/academicterms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsThreeSeededTerms_FilteredByYear()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var seededYear = years!.Single(y => y.Name == "2025-2026");

        var terms = await client.GetFromJsonAsync<List<AcademicTermDto>>($"/api/academicterms?academicYearId={seededYear.Id}");

        Assert.NotNull(terms);
        Assert.Equal(3, terms!.Count);
        Assert.Equal(new[] { "Trimestre 1", "Trimestre 2", "Trimestre 3" }, terms.OrderBy(t => t.Order).Select(t => t.Name));
    }

    [Fact]
    public async Task Director_CanCreateUpdateAndDeleteTerm()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var seededYear = years!.Single(y => y.Name == "2025-2026");

        var createResponse = await client.PostAsJsonAsync("/api/academicterms", new CreateAcademicTermRequest(
            "Trimestre Test", 99, DateTime.UtcNow, DateTime.UtcNow.AddMonths(3), seededYear.Id));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AcademicTermDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/academicterms/{created!.Id}", new UpdateAcademicTermRequest(
            "Trimestre Test Renommé", 99, created.StartDate, created.EndDate));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AcademicTermDto>();
        Assert.Equal("Trimestre Test Renommé", updated!.Name);

        var deleteResponse = await client.DeleteAsync($"/api/academicterms/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotCreateTerm()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var seededYear = years!.First();

        var response = await client.PostAsJsonAsync("/api/academicterms", new CreateAcademicTermRequest(
            "Trimestre Interdit", 100, DateTime.UtcNow, DateTime.UtcNow.AddMonths(3), seededYear.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
