using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.AssessmentGroups;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AssessmentGroupsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AssessmentGroupsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededGroups_ForTrimestre1()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var groups = await client.GetFromJsonAsync<List<AssessmentGroupDto>>("/api/assessmentgroups");

        Assert.NotNull(groups);
        Assert.Contains(groups!, g => g.Name == "Devoirs" && g.Weightage == 40);
        Assert.Contains(groups!, g => g.Name == "Compositions" && g.Weightage == 60);
    }

    [Fact]
    public async Task Director_CanCreateAndDeleteGroup()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var termsResponse = await client.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.AcademicTerms.AcademicTermDto>>(
            $"/api/academicterms?academicYearId={years!.Single(y => y.IsCurrent).Id}");
        var termId = termsResponse!.First().Id;

        var createResponse = await client.PostAsJsonAsync("/api/assessmentgroups", new CreateAssessmentGroupRequest("Oraux", 20, termId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AssessmentGroupDto>();

        var deleteResponse = await client.DeleteAsync($"/api/assessmentgroups/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateGroup()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/assessmentgroups", new CreateAssessmentGroupRequest("Interdit", 10, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
