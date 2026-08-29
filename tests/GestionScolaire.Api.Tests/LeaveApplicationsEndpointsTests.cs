using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.LeaveApplications;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class LeaveApplicationsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public LeaveApplicationsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_SeesSeededPendingAndApprovedApplications()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var all = await client.GetFromJsonAsync<List<LeaveApplicationDto>>("/api/leaveapplications");

        Assert.NotNull(all);
        Assert.Contains(all!, l => l.Status == "Pending");
        Assert.Contains(all!, l => l.Status == "Approved");
    }

    [Fact]
    public async Task Director_CanApprove_NewlyCreatedApplication()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var createResponse = await client.PostAsJsonAsync("/api/leaveapplications", new CreateLeaveApplicationRequest(
            students!.First().Id, DateTime.UtcNow.Date.AddDays(20), DateTime.UtcNow.Date.AddDays(21), "Test approbation"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<LeaveApplicationDto>();

        var response = await client.PutAsJsonAsync($"/api/leaveapplications/{created!.Id}/decide", new DecideLeaveApplicationRequest(true, "OK"));
        response.EnsureSuccessStatusCode();
        var decided = await response.Content.ReadFromJsonAsync<LeaveApplicationDto>();

        Assert.Equal("Approved", decided!.Status);
        Assert.NotNull(decided.DecisionDate);
    }

    [Fact]
    public async Task Parent_CanCreateLeaveApplication_ForOwnChild_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.PostAsJsonAsync("/api/leaveapplications", new CreateLeaveApplicationRequest(
            ownChild.Id, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(2), "Rendez-vous médical"));
        ownResponse.EnsureSuccessStatusCode();
        var created = await ownResponse.Content.ReadFromJsonAsync<LeaveApplicationDto>();
        Assert.Equal("Pending", created!.Status);

        var forbiddenResponse = await parent1.PostAsJsonAsync("/api/leaveapplications", new CreateLeaveApplicationRequest(
            otherChild.Id, DateTime.UtcNow.Date.AddDays(1), DateTime.UtcNow.Date.AddDays(2), "Interdit"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateLeaveApplication()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        var response = await client.PostAsJsonAsync("/api/leaveapplications", new CreateLeaveApplicationRequest(
            students!.First().Id, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), "Interdit"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_SeesLeaveApplications_OnlyForOwnClassStudents()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var ownClassStudentIds = (await teacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!
            .Select(s => s.Id)
            .ToHashSet();

        var applications = await teacherClient.GetFromJsonAsync<List<LeaveApplicationDto>>("/api/leaveapplications");

        Assert.NotNull(applications);
        Assert.All(applications!, a => Assert.Contains(a.StudentId, ownClassStudentIds));

        var allApplications = await directorClient.GetFromJsonAsync<List<LeaveApplicationDto>>("/api/leaveapplications");
        var expectedCount = allApplications!.Count(a => ownClassStudentIds.Contains(a.StudentId));
        Assert.Equal(expectedCount, applications!.Count);
    }
}
