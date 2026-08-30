using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.FinalGrades;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class FinalGradesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public FinalGradesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_GetByClass_ReturnsRankedResults_ForOwnClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var results = await client.GetFromJsonAsync<List<FinalGradeDto>>($"/api/finalgrades/class/{classId}?term=Trimestre 1");

        Assert.NotNull(results);
        Assert.Equal(students!.Count, results!.Count);
        Assert.All(results, r => Assert.Equal(students.Count, r.ClassSize));
        Assert.Equal(Enumerable.Range(1, students.Count), results.Select(r => r.ClassRank).OrderBy(x => x));
        Assert.All(results, r => Assert.NotNull(r.LetterGrade));
    }

    [Fact]
    public async Task Teacher_CannotGetByClass_ForOtherClass()
    {
        var mathTeacher = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var ownClassId = (await mathTeacher.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;
        var otherClassId = (await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!
            .First(s => s.ClassId != ownClassId).ClassId;

        var response = await mathTeacher.GetAsync($"/api/finalgrades/class/{otherClassId}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_GetByCourse_ReturnsPerCourseAggregates_ForOwnClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var classId = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;

        var report = await client.GetFromJsonAsync<List<CourseWiseAssessmentDto>>($"/api/finalgrades/class/{classId}/by-course?term=Trimestre 1");

        Assert.NotNull(report);
        Assert.NotEmpty(report!);
        Assert.All(report, c => Assert.True(c.StudentsEvaluated > 0));
        Assert.All(report, c => Assert.True(c.MinAverage <= c.ClassAverage && c.ClassAverage <= c.MaxAverage));
    }

    [Fact]
    public async Task Teacher_CannotGetByCourse_ForOtherClass()
    {
        var mathTeacher = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var otherClassId = (await frenchTeacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;

        var response = await mathTeacher.GetAsync($"/api/finalgrades/class/{otherClassId}/by-course?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanGetByStudent_ForOwnChild_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.GetAsync($"/api/finalgrades/student/{ownChild.Id}?term=Trimestre 1");
        ownResponse.EnsureSuccessStatusCode();
        var own = await ownResponse.Content.ReadFromJsonAsync<FinalGradeDto>();
        Assert.Equal(ownChild.Id, own!.StudentId);
        Assert.True(own.ClassRank >= 1 && own.ClassRank <= own.ClassSize);

        var otherResponse = await parent1.GetAsync($"/api/finalgrades/student/{otherChild.Id}?term=Trimestre 1");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }
}
