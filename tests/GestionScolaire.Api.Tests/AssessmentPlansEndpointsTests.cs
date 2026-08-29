using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AssessmentGroups;
using GestionScolaire.Application.DTOs.AssessmentPlans;
using GestionScolaire.Application.DTOs.Courses;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AssessmentPlansEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AssessmentPlansEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededPlan_WithTwoCriteria()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var plans = await client.GetFromJsonAsync<List<AssessmentPlanDto>>("/api/assessmentplans");

        Assert.NotNull(plans);
        var seeded = Assert.Single(plans!, p => p.Name.Contains("Mathématiques"));
        Assert.Equal(2, seeded.Criteria.Count);
        Assert.Equal("Completed", seeded.Status);
    }

    [Fact]
    public async Task Director_NewlyCreatedPlan_DefaultsToDraft_AndCanBeUpdated()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var groups = await client.GetFromJsonAsync<List<AssessmentGroupDto>>("/api/assessmentgroups");

        var createResponse = await client.PostAsJsonAsync("/api/assessmentplans", new CreateAssessmentPlanRequest(
            "Plan Test Statut", 20, DateTime.UtcNow.AddDays(5),
            courses!.First().Id, students!.First().ClassId, groups!.First().AcademicTermId, groups!.First().Id, null));
        var plan = await createResponse.Content.ReadFromJsonAsync<AssessmentPlanDto>();
        Assert.Equal("Draft", plan!.Status);

        var updateResponse = await client.PutAsJsonAsync($"/api/assessmentplans/{plan.Id}/status",
            new UpdateAssessmentPlanStatusRequest(AssessmentPlanStatus.Scheduled));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<AssessmentPlanDto>();
        Assert.Equal("Scheduled", updated!.Status);
    }

    [Fact]
    public async Task Teacher_CannotUpdatePlanStatus_ForOtherClass()
    {
        var mathTeacher = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacher = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var plans = await mathTeacher.GetFromJsonAsync<List<AssessmentPlanDto>>("/api/assessmentplans");
        var seeded = plans!.Single(p => p.Name.Contains("Mathématiques"));

        var response = await frenchTeacher.PutAsJsonAsync($"/api/assessmentplans/{seeded.Id}/status",
            new UpdateAssessmentPlanStatusRequest(AssessmentPlanStatus.Draft));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanCreatePlan_ForOwnClass_ButNotOtherClass()
    {
        var mathTeacher = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var ownClassId = (await mathTeacher.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;
        var courses = await mathTeacher.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var groups = await mathTeacher.GetFromJsonAsync<List<AssessmentGroupDto>>("/api/assessmentgroups");

        var ownResponse = await mathTeacher.PostAsJsonAsync("/api/assessmentplans", new CreateAssessmentPlanRequest(
            "Devoir surveillé", 20, DateTime.UtcNow.AddDays(5),
            courses!.First().Id, ownClassId, groups!.First().AcademicTermId, groups!.First().Id, null));
        ownResponse.EnsureSuccessStatusCode();
        var created = await ownResponse.Content.ReadFromJsonAsync<AssessmentPlanDto>();
        Assert.Equal("Devoir surveillé", created!.Name);

        var frenchTeacher = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");
        var otherClassResponse = await frenchTeacher.PostAsJsonAsync("/api/assessmentplans", new CreateAssessmentPlanRequest(
            "Interdit", 20, DateTime.UtcNow.AddDays(5),
            courses!.First().Id, ownClassId, groups!.First().AcademicTermId, groups!.First().Id, null));

        Assert.Equal(HttpStatusCode.Forbidden, otherClassResponse.StatusCode);
    }

    [Fact]
    public async Task Director_CanAddCriteria_ToNewlyCreatedPlan()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var groups = await client.GetFromJsonAsync<List<AssessmentGroupDto>>("/api/assessmentgroups");

        var createResponse = await client.PostAsJsonAsync("/api/assessmentplans", new CreateAssessmentPlanRequest(
            "Plan Test Critères", 20, DateTime.UtcNow.AddDays(5),
            courses!.First().Id, students!.First().ClassId, groups!.First().AcademicTermId, groups!.First().Id, null));
        var plan = await createResponse.Content.ReadFromJsonAsync<AssessmentPlanDto>();

        var response = await client.PostAsJsonAsync($"/api/assessmentplans/{plan!.Id}/criteria",
            new CreateAssessmentCriteriaRequest("Participation", 3));

        response.EnsureSuccessStatusCode();
        var criteria = await response.Content.ReadFromJsonAsync<AssessmentCriteriaDto>();
        Assert.Equal("Participation", criteria!.Name);
    }
}
