using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.StudentLogs;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentLogsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentLogsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_CanCreateAndViewLog_ForOwnStudent()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var student = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var createResponse = await client.PostAsJsonAsync("/api/studentlogs", new CreateStudentLogRequest(
            student.Id, DateTime.UtcNow, "Académique", "Test note de suivi."));
        createResponse.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<StudentLogDto>>($"/api/studentlogs/student/{student.Id}");
        Assert.Contains(logs!, l => l.Description == "Test note de suivi.");
    }

    [Fact]
    public async Task Teacher_CannotCreateLog_ForStudentOutsideTheirClass()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var otherClassStudent = (await frenchTeacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var response = await mathTeacherClient.PostAsJsonAsync("/api/studentlogs", new CreateStudentLogRequest(
            otherClassStudent.Id, DateTime.UtcNow, "Académique", "Ne devrait pas passer."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanViewOwnChildLogs_ButCannotCreateOne()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var ownChild = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var getResponse = await parentClient.GetAsync($"/api/studentlogs/student/{ownChild.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var createResponse = await parentClient.PostAsJsonAsync("/api/studentlogs", new CreateStudentLogRequest(
            ownChild.Id, DateTime.UtcNow, "Général", "Un parent ne peut pas écrire dans le journal."));

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotViewAnotherChildLogs()
    {
        var parent1Client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2Client = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var otherChild = (await parent2Client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parent1Client.GetAsync($"/api/studentlogs/student/{otherChild.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
