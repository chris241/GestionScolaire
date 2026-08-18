using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Application.DTOs.Subjects;
using GestionScolaire.Domain.Enums;
using GestionScolaire.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class GradesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public GradesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(StudentDto Student, SubjectDto Subject)> GetOwnStudentAndSubjectAsync(HttpClient teacherClient)
    {
        var students = await teacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var subjects = await teacherClient.GetFromJsonAsync<List<SubjectDto>>("/api/subjects");
        return (students!.First(), subjects!.First());
    }

    [Fact]
    public async Task Teacher_CanCreateGrade_ForOwnStudent()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var (student, subject) = await GetOwnStudentAndSubjectAsync(teacherClient);

        var response = await teacherClient.PostAsJsonAsync("/api/grades", new CreateGradeRequest(
            student.Id, subject.Id, student.ClassId, 15, 20, 2, EvaluationType.Devoir, "Trimestre 1", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grade = await response.Content.ReadFromJsonAsync<GradeDto>();
        Assert.NotNull(grade);
        Assert.Equal(15, grade!.Score);
    }

    [Fact]
    public async Task Teacher_CannotCreateGrade_ForStudentOutsideTheirClass()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var (otherClassStudent, subject) = await GetOwnStudentAndSubjectAsync(frenchTeacherClient);

        var response = await mathTeacherClient.PostAsJsonAsync("/api/grades", new CreateGradeRequest(
            otherClassStudent.Id, subject.Id, otherClassStudent.ClassId, 15, 20, 2, EvaluationType.Devoir, "Trimestre 1", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotViewGrades_ForStudentOutsideTheirClass()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var (otherClassStudent, _) = await GetOwnStudentAndSubjectAsync(frenchTeacherClient);

        var response = await mathTeacherClient.GetAsync($"/api/grades/student/{otherClassStudent.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotCreateGrade_EvenForOwnChild()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var (ownChild, subject) = await GetOwnStudentAndSubjectAsync(parentClient);

        var response = await parentClient.PostAsJsonAsync("/api/grades", new CreateGradeRequest(
            ownChild.Id, subject.Id, ownChild.ClassId, 15, 20, 2, EvaluationType.Devoir, "Trimestre 1", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanViewGrades_ForOwnChild()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");
        var (ownChild, _) = await GetOwnStudentAndSubjectAsync(parentClient);

        var response = await parentClient.GetAsync($"/api/grades/student/{ownChild.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotViewGrades_ForAnotherChild()
    {
        var parent1Client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2Client = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var (otherChild, _) = await GetOwnStudentAndSubjectAsync(parent2Client);

        var response = await parent1Client.GetAsync($"/api/grades/student/{otherChild.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_CanCreateGrade_ForAnyStudent_WithExplicitTeacherId()
    {
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var (student, subject) = await GetOwnStudentAndSubjectAsync(teacherClient);

        // Le DTO expose l'Id du User, pas celui de l'entité Teacher (FK attendue par Grade.TeacherId) :
        // on va donc chercher le véritable Teacher.Id directement en base pour ce test.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var teacherId = await db.Teachers
            .Where(t => t.User.Email == "prof.francais@ecole.mg")
            .Select(t => t.Id)
            .SingleAsync();

        var response = await directorClient.PostAsJsonAsync("/api/grades", new CreateGradeRequest(
            student.Id, subject.Id, student.ClassId, 18, 20, 1, EvaluationType.Examen, "Trimestre 1", null, teacherId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GeneralAverage_ReflectsNewlyCreatedGrade_ConsistentlyWithGradeHistory()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var students = await teacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var subjects = await teacherClient.GetFromJsonAsync<List<SubjectDto>>("/api/subjects");

        var student = students!.First();
        var subject = subjects!.First();

        var createResponse = await teacherClient.PostAsJsonAsync("/api/grades", new CreateGradeRequest(
            student.Id, subject.Id, student.ClassId, 20, 20, 5, EvaluationType.Examen, "Trimestre Test", null));
        createResponse.EnsureSuccessStatusCode();

        var allGrades = await teacherClient.GetFromJsonAsync<List<GradeDto>>($"/api/grades/student/{student.Id}");
        var average = await teacherClient.GetFromJsonAsync<StudentGeneralAverageDto>($"/api/grades/student/{student.Id}/average");

        var subjectGrades = allGrades!.Where(g => g.SubjectName == subject.Name).ToList();
        var expectedSubjectAverage = Math.Round(
            subjectGrades.Sum(g => g.Score / g.MaxScore * 20 * g.Coefficient) / subjectGrades.Sum(g => g.Coefficient),
            2);

        Assert.NotNull(average);
        var actualSubjectAverage = average!.SubjectAverages.Single(s => s.SubjectName == subject.Name).Average;
        Assert.Equal(expectedSubjectAverage, actualSubjectAverage);
    }
}
