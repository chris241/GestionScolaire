using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.CourseSchedules;
using GestionScolaire.Application.DTOs.Courses;
using GestionScolaire.Application.DTOs.Rooms;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Application.DTOs.Teachers;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class CourseSchedulesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public CourseSchedulesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededSchedules()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var schedules = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");

        Assert.NotNull(schedules);
        Assert.Equal(5, schedules!.Count);
    }

    [Fact]
    public async Task Director_CannotDoubleBook_SameRoomTermDayAndStartTime()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var existing = (await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules"))!.First();
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var otherTeacher = teachers!.First(t => t.Id != existing.TeacherId);

        var conflictResponse = await client.PostAsJsonAsync("/api/courseschedules", new CreateCourseScheduleRequest(
            existing.CourseId, existing.RoomId, otherTeacher.Id, existing.ClassId,
            existing.AcademicTermId, existing.DayOfWeek, existing.StartTime, existing.EndTime));

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task Director_CanCreateSchedule_OnFreeSlot()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var rooms = await client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var term = existing!.First().AcademicTermId;

        var createResponse = await client.PostAsJsonAsync("/api/courseschedules", new CreateCourseScheduleRequest(
            courses!.First().Id, rooms!.First().Id, teachers!.First().Id, null,
            term, DayOfWeek.Saturday, new TimeOnly(14, 0), new TimeOnly(15, 0)));

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CourseScheduleDto>();
        Assert.Equal(DayOfWeek.Saturday, created!.DayOfWeek);
    }

    [Fact]
    public async Task Parent_CanViewOwnChildSchedule()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var child = students!.Single();

        var response = await client.GetAsync($"/api/courseschedules/student/{child.Id}");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Teacher_CannotCreateSchedule()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var rooms = await client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");

        var response = await client.PostAsJsonAsync("/api/courseschedules", new CreateCourseScheduleRequest(
            courses!.First().Id, rooms!.First().Id, teachers!.First().Id, null,
            existing!.First().AcademicTermId, DayOfWeek.Saturday, new TimeOnly(16, 0), new TimeOnly(17, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
