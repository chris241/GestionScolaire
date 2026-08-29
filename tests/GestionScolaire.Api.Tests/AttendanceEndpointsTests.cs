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

    [Fact]
    public async Task Director_GetAbsentReport_ReturnsOnlyNonPresentStudents_ForMarkedDate()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var classStudents = students!.Where(s => s.ClassId == classId).ToList();
        var date = new DateTime(2031, 3, 16);

        var entries = classStudents.Select((s, i) =>
            new AttendanceEntryRequest(s.Id, i % 2 == 0 ? AttendanceStatus.Absent : AttendanceStatus.Present, null)).ToList();
        await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(classId, date, entries));

        var report = await client.GetFromJsonAsync<List<AbsentStudentDto>>(
            $"/api/attendance/reports/absent?date={date:yyyy-MM-dd}&classId={classId}");

        Assert.NotNull(report);
        var expectedAbsentCount = entries.Count(e => e.Status == AttendanceStatus.Absent);
        Assert.Equal(expectedAbsentCount, report!.Count);
        Assert.All(report, r => Assert.Equal("Absent", r.Status));
    }

    [Fact]
    public async Task Teacher_GetAbsentReport_WithoutClassId_IsScopedToOwnClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var ownStudents = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var date = new DateTime(2031, 3, 17);

        var entries = ownStudents!.Select(s => new AttendanceEntryRequest(s.Id, AttendanceStatus.Retard, null)).ToList();
        await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(ownStudents!.First().ClassId, date, entries));

        var report = await client.GetFromJsonAsync<List<AbsentStudentDto>>($"/api/attendance/reports/absent?date={date:yyyy-MM-dd}");

        Assert.NotNull(report);
        var ownIds = ownStudents!.Select(s => s.Id).ToHashSet();
        Assert.All(report!, r => Assert.Contains(r.StudentId, ownIds));
    }

    [Fact]
    public async Task Teacher_CannotGetAbsentReport_ForOtherClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var ownClassId = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;
        var otherClassId = (await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!
            .First(s => s.ClassId != ownClassId).ClassId;

        var response = await client.GetAsync($"/api/attendance/reports/absent?date=2031-03-16&classId={otherClassId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_GetMonthlySheet_ReflectsMarkedDay()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var classStudents = students!.Where(s => s.ClassId == classId).ToList();
        var date = new DateTime(2031, 3, 15);

        var entries = classStudents.Select(s => new AttendanceEntryRequest(s.Id, AttendanceStatus.Excuse, null)).ToList();
        await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(classId, date, entries));

        var sheet = await client.GetFromJsonAsync<List<MonthlyAttendanceRowDto>>(
            $"/api/attendance/reports/monthly?classId={classId}&year=2031&month=3");

        Assert.NotNull(sheet);
        Assert.Equal(classStudents.Count, sheet!.Count);
        Assert.All(sheet, row => Assert.Equal("Excuse", row.DayStatuses[15]));
    }

    [Fact]
    public async Task Teacher_CannotGetMonthlySheet_ForOtherClass()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var ownClassId = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;
        var otherClassId = (await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!
            .First(s => s.ClassId != ownClassId).ClassId;

        var response = await client.GetAsync($"/api/attendance/reports/monthly?classId={otherClassId}&year=2031&month=3");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_GetBatchSummary_CountsMarkedEntries_ForSeededBatch()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var target = students!.First(s => s.ClassId == classId);

        var day1 = new DateTime(2031, 3, 15);
        var day2 = new DateTime(2031, 3, 16);
        await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(
            classId, day1, new List<AttendanceEntryRequest> { new(target.Id, AttendanceStatus.Present, null) }));
        await client.PostAsJsonAsync("/api/attendance/bulk", new BulkMarkAttendanceRequest(
            classId, day2, new List<AttendanceEntryRequest> { new(target.Id, AttendanceStatus.Absent, null) }));

        var summaries = await client.GetFromJsonAsync<List<BatchAttendanceSummaryDto>>(
            "/api/attendance/reports/batch-summary?startDate=2031-03-15&endDate=2031-03-16");

        Assert.NotNull(summaries);
        var batch = Assert.Single(summaries!);
        var studentSummary = batch.Students.Single(s => s.StudentId == target.Id);
        Assert.Equal(1, studentSummary.PresentCount);
        Assert.Equal(1, studentSummary.AbsentCount);
        Assert.Equal(2, studentSummary.TotalRecorded);
    }

    [Fact]
    public async Task Teacher_CannotAccessBatchSummary()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/attendance/reports/batch-summary?startDate=2031-03-15&endDate=2031-03-16");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
