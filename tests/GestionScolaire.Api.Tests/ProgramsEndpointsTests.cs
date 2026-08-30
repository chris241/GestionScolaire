using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Programs;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class ProgramsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public ProgramsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededProgram_WithClassesAndCourses()
    {
        // AcademicProgram est scopé par école : un Parent (sans contexte école) ne peut plus le lister,
        // contrairement à un Directeur, qui ne voit que le programme de son école active.
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var programs = await client.GetFromJsonAsync<List<ProgramDto>>("/api/programs");

        Assert.NotNull(programs);
        var seeded = Assert.Single(programs!, p => p.Code == "COL-GEN");
        Assert.Equal(2, seeded.ClassCount);
        Assert.Equal(5, seeded.CourseCount);
    }

    [Fact]
    public async Task Director_CanCreateAndDeleteProgram()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/programs", new CreateProgramRequest(
            "Lycée Général", "LYC-GEN", "Filière générale du lycée"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramDto>();

        Assert.Equal("Lycée Général", created!.Name);

        var deleteResponse = await client.DeleteAsync($"/api/programs/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateProgram()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/programs", new CreateProgramRequest(
            "Interdit", "INT", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
