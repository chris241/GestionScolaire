using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Application.DTOs.Invoices;
using GestionScolaire.Application.DTOs.Payments;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentPortalEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentPortalEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Student_SeesOnlyOwnRecord_InStudentsList()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        Assert.NotNull(students);
        var self = Assert.Single(students!);
        Assert.Equal("Tojo", self.FirstName);
    }

    [Fact]
    public async Task Student_CanViewOwnGrades_AverageAndBulletin()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var gradesResponse = await client.GetAsync($"/api/grades/student/{self.Id}");
        gradesResponse.EnsureSuccessStatusCode();
        var grades = await gradesResponse.Content.ReadFromJsonAsync<List<GradeDto>>();
        Assert.NotEmpty(grades!);

        var averageResponse = await client.GetAsync($"/api/grades/student/{self.Id}/average");
        averageResponse.EnsureSuccessStatusCode();

        var bulletinResponse = await client.GetAsync($"/api/bulletins/student/{self.Id}?term=Trimestre 1");
        Assert.Equal(HttpStatusCode.OK, bulletinResponse.StatusCode);
        Assert.Equal("application/pdf", bulletinResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Student_CanViewOwnPaymentsAndInvoices_ButNotGlobalLists()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownPaymentsResponse = await client.GetAsync($"/api/payments/student/{self.Id}");
        ownPaymentsResponse.EnsureSuccessStatusCode();
        var payments = await ownPaymentsResponse.Content.ReadFromJsonAsync<List<PaymentDto>>();
        Assert.NotEmpty(payments!);

        var ownInvoicesResponse = await client.GetAsync($"/api/invoices/student/{self.Id}");
        ownInvoicesResponse.EnsureSuccessStatusCode();

        var globalPaymentsResponse = await client.GetAsync("/api/payments");
        Assert.Equal(HttpStatusCode.Forbidden, globalPaymentsResponse.StatusCode);

        var globalInvoicesResponse = await client.GetAsync("/api/invoices");
        Assert.Equal(HttpStatusCode.Forbidden, globalInvoicesResponse.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnAttendanceAndSchedule()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var attendanceResponse = await client.GetAsync($"/api/attendance/student/{self.Id}");
        attendanceResponse.EnsureSuccessStatusCode();

        var scheduleResponse = await client.GetAsync($"/api/courseschedules/student/{self.Id}");
        scheduleResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Student_CannotAccessAnotherStudentsRecords()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherStudent = (await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!
            .First(s => s.Id != self.Id);

        var response = await client.GetAsync($"/api/grades/student/{otherStudent.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotAccessDirectorOnlyDashboard()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");

        var response = await client.GetAsync("/api/dashboard/stats");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotCreateLeaveApplication()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await client.PostAsJsonAsync("/api/leaveapplications", new
        {
            StudentId = self.Id,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Reason = "Interdit"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
