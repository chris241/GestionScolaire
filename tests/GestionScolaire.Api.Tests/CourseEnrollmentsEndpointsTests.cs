using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.CourseEnrollments;
using GestionScolaire.Application.DTOs.Courses;
using GestionScolaire.Application.DTOs.ProgramEnrollments;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

// Sélectionne toujours les cours par nom (jamais par position) : la suite partage une seule base entre
// toutes les classes de test exécutées séquentiellement, et l'un des tests ci-dessous crée un cours
// supplémentaire dont le nom peut se glisser n'importe où dans le tri alphabétique global de /api/courses.
[Collection(ApiTestCollection.Name)]
public class CourseEnrollmentsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public CourseEnrollmentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<CourseDto> GetCourseByNameAsync(HttpClient client, string name)
    {
        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        return courses!.Single(c => c.Name == name);
    }

    [Fact]
    public async Task Director_CanEnrollStudent_WhoHasMatchingProgramEnrollment()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var course = await GetCourseByNameAsync(client, "Anglais");
        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var enrollment = enrollments!.First();

        var response = await client.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            enrollment.StudentId, course.Id, enrollment.AcademicYearId));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CourseEnrollmentDto>();
        Assert.Equal("Active", created!.Status);
        Assert.Equal(course.Id, created.CourseId);
    }

    [Fact]
    public async Task Director_CreateEnrollment_RejectsDuplicate()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var course = await GetCourseByNameAsync(client, "Français");
        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var enrollment = enrollments!.First();

        var firstResponse = await client.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            enrollment.StudentId, course.Id, enrollment.AcademicYearId));
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            enrollment.StudentId, course.Id, enrollment.AcademicYearId));

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Director_CreateEnrollment_RejectsStudent_WithoutMatchingProgramEnrollment()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        // Programme et cours isolés, auxquels aucun élève n'est inscrit.
        var programResponse = await client.PostAsJsonAsync("/api/programs", new CreateProgramRequest(
            "Programme Isolé Test", "ISO-TEST", null));
        var isolatedProgram = await programResponse.Content.ReadFromJsonAsync<ProgramDto>();

        var anySubjectCourse = await GetCourseByNameAsync(client, "Sciences");

        var courseResponse = await client.PostAsJsonAsync("/api/courses", new CreateCourseRequest(
            "Cours Isolé Test", null, null, anySubjectCourse.SubjectId, isolatedProgram!.Id));
        var isolatedCourse = await courseResponse.Content.ReadFromJsonAsync<CourseDto>();

        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var unrelatedStudentEnrollment = enrollments!.First();

        var response = await client.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            unrelatedStudentEnrollment.StudentId, isolatedCourse!.Id, unrelatedStudentEnrollment.AcademicYearId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Director_BulkEnroll_OnlyEnrollsEligibleStudents_AndIsIdempotent()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var course = await GetCourseByNameAsync(client, "Histoire-Géographie");
        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var academicYearId = enrollments!.First().AcademicYearId;
        var studentIds = enrollments!.Select(e => e.StudentId).ToList();

        var response = await client.PostAsJsonAsync("/api/courseenrollments/bulk", new BulkCourseEnrollRequest(
            studentIds, course.Id, academicYearId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<CourseEnrollmentDto>>();
        Assert.Equal(studentIds.Count, result!.Count);

        var secondResponse = await client.PostAsJsonAsync("/api/courseenrollments/bulk", new BulkCourseEnrollRequest(
            studentIds, course.Id, academicYearId));
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<List<CourseEnrollmentDto>>();
        Assert.Equal(studentIds.Count, secondResult!.Count);
    }

    [Fact]
    public async Task Student_CanViewOwnCourseEnrollments_ButNotAnotherStudents()
    {
        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var course = await GetCourseByNameAsync(director, "Mathématiques");

        var studentClient = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await studentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var enrollments = await director.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");
        var ownEnrollment = enrollments!.Single(e => e.StudentId == self.Id);

        await director.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            self.Id, course.Id, ownEnrollment.AcademicYearId));

        var ownResponse = await studentClient.GetAsync($"/api/courseenrollments/student/{self.Id}");
        ownResponse.EnsureSuccessStatusCode();
        var ownList = await ownResponse.Content.ReadFromJsonAsync<List<CourseEnrollmentDto>>();
        Assert.Contains(ownList!, e => e.CourseId == course.Id);

        var otherEnrollment = enrollments!.First(e => e.StudentId != self.Id);
        var otherResponse = await studentClient.GetAsync($"/api/courseenrollments/student/{otherEnrollment.StudentId}");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateEnrollment()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var course = await GetCourseByNameAsync(director, "Anglais");
        var enrollments = await director.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");

        var response = await client.PostAsJsonAsync("/api/courseenrollments", new CreateCourseEnrollmentRequest(
            enrollments!.First().StudentId, course.Id, enrollments!.First().AcademicYearId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotListAllEnrollments()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var response = await client.GetAsync("/api/courseenrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
