using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.TeacherLogs;
using GestionScolaire.Application.DTOs.Teachers;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class TeacherLogsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public TeacherLogsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_CanCreateAndViewLog_ForAnyTeacher()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teacher = (await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers"))!.First();

        var createResponse = await client.PostAsJsonAsync("/api/teacherlogs", new CreateTeacherLogRequest(
            teacher.Id, DateTime.UtcNow, "Évaluation", "Note de suivi annuelle."));
        createResponse.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<TeacherLogDto>>($"/api/teacherlogs/teacher/{teacher.Id}");
        Assert.Contains(logs!, l => l.Description == "Note de suivi annuelle.");
    }

    [Fact]
    public async Task Teacher_CanCreateAndViewOwnLog_ButNotAnotherTeachersLog()
    {
        var mathTeacher = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacher = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teachers = await director.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var mathTeacherEntry = teachers!.Single(t => t.Email == "prof.math@ecole.mg");
        var frenchTeacherEntry = teachers!.Single(t => t.Email == "prof.francais@ecole.mg");

        var ownResponse = await mathTeacher.PostAsJsonAsync("/api/teacherlogs", new CreateTeacherLogRequest(
            mathTeacherEntry.Id, DateTime.UtcNow, "Formation", "Auto-note."));
        ownResponse.EnsureSuccessStatusCode();

        var otherResponse = await mathTeacher.PostAsJsonAsync("/api/teacherlogs", new CreateTeacherLogRequest(
            frenchTeacherEntry.Id, DateTime.UtcNow, "Incident", "Ne devrait pas passer."));
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);

        var getOtherResponse = await mathTeacher.GetAsync($"/api/teacherlogs/teacher/{frenchTeacherEntry.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getOtherResponse.StatusCode);

        _ = frenchTeacher;
    }

    [Fact]
    public async Task Parent_CannotAccessTeacherLogs()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teacher = (await director.GetFromJsonAsync<List<TeacherDto>>("/api/teachers"))!.First();

        var response = await parentClient.GetAsync($"/api/teacherlogs/teacher/{teacher.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
