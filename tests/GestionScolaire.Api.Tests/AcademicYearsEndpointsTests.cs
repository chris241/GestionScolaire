using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AcademicYearsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AcademicYearsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/academicyears");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsSeededYear_ForRoleWithSchoolContext()
    {
        // AcademicYear est scopée par école : un Parent (sans contexte école) ne peut plus lister
        // les années académiques, contrairement à un Enseignant rattaché à une école.
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");

        Assert.NotNull(years);
        // N'affirme pas IsCurrent : un autre test peut légitimement changer l'année courante (voir plus bas).
        Assert.Contains(years!, y => y.Name == "2025-2026");
    }

    [Fact]
    public async Task GetCurrent_ReturnsTheSeededCurrentYear()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var year = await client.GetFromJsonAsync<AcademicYearDto>("/api/academicyears/current");

        Assert.NotNull(year);
        Assert.True(year!.IsCurrent);
    }

    [Fact]
    public async Task Director_CanCreateAndSetCurrent()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/academicyears", new CreateAcademicYearRequest(
            "2026-2027", new DateTime(2026, 9, 1), new DateTime(2027, 6, 30)));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AcademicYearDto>();

        var setCurrentResponse = await client.PostAsync($"/api/academicyears/{created!.Id}/set-current", null);
        Assert.Equal(HttpStatusCode.NoContent, setCurrentResponse.StatusCode);

        var current = await client.GetFromJsonAsync<AcademicYearDto>("/api/academicyears/current");
        Assert.Equal(created.Id, current!.Id);
    }

    [Fact]
    public async Task Teacher_CannotCreateAcademicYear()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/academicyears", new CreateAcademicYearRequest(
            "2027-2028", new DateTime(2027, 9, 1), new DateTime(2028, 6, 30)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
