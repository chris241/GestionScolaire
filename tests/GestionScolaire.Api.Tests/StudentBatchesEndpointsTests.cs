using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.StudentBatches;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentBatchesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentBatchesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededBatch()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var batches = await client.GetFromJsonAsync<List<StudentBatchDto>>("/api/studentbatches");

        Assert.NotNull(batches);
        Assert.Contains(batches!, b => b.Name == "Promotion 2025-2026");
    }

    [Fact]
    public async Task Director_CanCreateAndDeleteBatch()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);

        var createResponse = await client.PostAsJsonAsync("/api/studentbatches", new CreateStudentBatchRequest(
            "Promotion Test", DateTime.UtcNow, null, null, currentYear.Id));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentBatchDto>();

        var deleteResponse = await client.DeleteAsync($"/api/studentbatches/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotCreateBatch()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");

        var response = await client.PostAsJsonAsync("/api/studentbatches", new CreateStudentBatchRequest(
            "Interdit", DateTime.UtcNow, null, null, years!.First().Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
