using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AdmissionsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AdmissionsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/studentapplicants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Director_SeesSeededApplicants_AcrossStatuses()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var applicants = await client.GetFromJsonAsync<List<StudentApplicantDto>>("/api/studentapplicants");

        Assert.NotNull(applicants);
        Assert.Contains(applicants!, a => a.Status == "Submitted");
        Assert.Contains(applicants!, a => a.Status == "UnderReview");
        Assert.Contains(applicants!, a => a.Status == "Rejected");
    }

    [Fact]
    public async Task Teacher_CannotAccessApplicants()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/studentapplicants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotAccessApplicants()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var response = await client.GetAsync("/api/studentapplicants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FullWorkflow_CreateReviewAccept_ConvertsToRealStudent()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var studentsBefore = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var countBefore = studentsBefore!.Count;
        var targetClassId = studentsBefore!.First().ClassId;

        var createResponse = await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
            "Nouveau", "Candidat", new DateTime(2014, 5, 1), Gender.Masculin,
            null, null, "Parent Test", null, "034 12 345 67", "6ème", currentYear.Id, null, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();
        Assert.Equal("Submitted", created!.Status);

        var reviewResponse = await client.PutAsJsonAsync($"/api/studentapplicants/{created.Id}/status",
            new UpdateStudentApplicantStatusRequest(AdmissionStatus.UnderReview, "En cours d'analyse"));
        reviewResponse.EnsureSuccessStatusCode();
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();
        Assert.Equal("UnderReview", reviewed!.Status);

        var acceptResponse = await client.PostAsJsonAsync($"/api/studentapplicants/{created.Id}/accept",
            new AcceptApplicantRequest(targetClassId, null));
        acceptResponse.EnsureSuccessStatusCode();
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();

        Assert.Equal("Enrolled", accepted!.Status);
        Assert.NotNull(accepted.ConvertedStudentId);

        var studentsAfter = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        Assert.Equal(countBefore + 1, studentsAfter!.Count);
        Assert.Contains(studentsAfter, s => s.Id == accepted.ConvertedStudentId);
    }

    [Fact]
    public async Task Reject_SetsStatusToRejected()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);

        var createResponse = await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
            "À Refuser", "Candidat", new DateTime(2014, 5, 1), Gender.Feminin,
            null, null, null, null, null, "5ème", currentYear.Id, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();

        var rejectResponse = await client.PostAsJsonAsync($"/api/studentapplicants/{created!.Id}/reject", "Dossier incomplet");
        rejectResponse.EnsureSuccessStatusCode();
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();

        Assert.Equal("Rejected", rejected!.Status);
    }

    [Fact]
    public async Task Accept_Twice_ReturnsBadRequest()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var createResponse = await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
            "Double", "Acceptation", new DateTime(2014, 5, 1), Gender.Masculin,
            null, null, null, null, null, "6ème", currentYear.Id, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicantDto>();

        await client.PostAsJsonAsync($"/api/studentapplicants/{created!.Id}/accept", new AcceptApplicantRequest(classId, null));
        var secondAcceptResponse = await client.PostAsJsonAsync($"/api/studentapplicants/{created.Id}/accept", new AcceptApplicantRequest(classId, null));

        Assert.Equal(HttpStatusCode.BadRequest, secondAcceptResponse.StatusCode);
    }
}
