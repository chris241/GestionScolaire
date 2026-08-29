using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Attendances;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AttendanceEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AttendanceEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_GetByClass_ReturnsOneRowPerStudent_ForOwnClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var records = await client.GetFromJsonAsync<List<AttendanceDto>>($"/api/attendance?classId={classId}&date={today}");

        Assert.NotNull(records);
        Assert.Equal(students!.Count, records!.Count);
        Assert.All(records, r => Assert.NotNull(r.Status));
    }

    [Fact]
    public async Task Teacher_CannotAccessOtherClassAttendance()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var allStudents = await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var ownClassId = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;
        var otherClassId = allStudents!.First(s => s.ClassId != ownClassId).ClassId;
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/attendance?classId={otherClassId}&date={today}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_CanBulkMarkAttendance_AndPersistStatuses()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var classStudents = students!.Where(s => s.ClassId == classId).ToList();
        var date = DateTime.UtcNow.Date.AddDays(-1);

        var entries = classStudents.Select(s => new AttendanceEntryRequest(s.Id, AttendanceStatus.Retard, "Test")).ToList();

        var bulkResponse = await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(classId, date, entries));
        bulkResponse.EnsureSuccessStatusCode();
        var updated = await bulkResponse.Content.ReadFromJsonAsync<List<AttendanceDto>>();

        Assert.Equal(classStudents.Count, updated!.Count);
        Assert.All(updated, r => Assert.Equal("Retard", r.Status));

        var refetched = await client.GetFromJsonAsync<List<AttendanceDto>>(
            $"/api/attendance?classId={classId}&date={date:yyyy-MM-dd}");
        Assert.All(refetched!, r => Assert.Equal("Retard", r.Status));
    }

    [Fact]
    public async Task Parent_CanViewOwnChildAttendance_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.GetAsync($"/api/attendance/student/{ownChild.Id}");
        ownResponse.EnsureSuccessStatusCode();

        var otherResponse = await parent1.GetAsync($"/api/attendance/student/{otherChild.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }
}
