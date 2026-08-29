using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Application.DTOs.ProgramEnrollments;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class ProgramEnrollmentsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public ProgramEnrollmentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededEnrollments_ForAllEightStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");

        Assert.NotNull(enrollments);
        Assert.Equal(8, enrollments!.Count);
        Assert.All(enrollments, e => Assert.Equal("Active", e.Status));
    }

    [Fact]
    public async Task Director_BulkEnroll_IsIdempotent_ForAlreadyEnrolledStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var programs = await client.GetFromJsonAsync<List<ProgramDto>>("/api/programs");
        var program = programs!.Single(p => p.Code == "COL-GEN");
        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>($"/api/programenrollments?programId={program.Id}");
        var academicYearId = enrollments!.First().AcademicYearId;
        var studentIds = enrollments!.Select(e => e.StudentId).ToList();

        var response = await client.PostAsJsonAsync("/api/programenrollments/bulk", new BulkEnrollRequest(studentIds, program.Id, academicYearId));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ProgramEnrollmentDto>>();
        Assert.Equal(8, result!.Count);
    }

    [Fact]
    public async Task Director_CreateEnrollment_RejectsDuplicate()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var existing = enrollments!.First();

        var response = await client.PostAsJsonAsync("/api/programenrollments", new CreateProgramEnrollmentRequest(
            existing.StudentId, existing.ProgramId, existing.AcademicYearId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotListEnrollments()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var response = await client.GetAsync("/api/programenrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
