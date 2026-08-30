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
        // >= plutôt que == : d'autres tests de ce fichier (assistant de planification) créent aussi des
        // séances réelles via /auto-plan/commit, dans une suite qui partage une seule base entre toutes
        // les classes de test exécutées séquentiellement.
        Assert.True(schedules!.Count >= 5);
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

    // Utilise systématiquement le samedi (absent du seed, qui ne planifie que Lundi-Vendredi), avec une
    // fenêtre horaire disjointe par test, pour garantir un créneau vierge et éviter toute collision avec
    // les séances déjà en base ou avec les autres tests de ce fichier (l'ordre d'exécution n'est pas garanti).
    [Fact]
    public async Task Director_AutoPlan_PlacesAllRequirements_WithoutAnyConflict()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var term = existing!.First().AcademicTermId;
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var response = await client.PostAsJsonAsync("/api/courseschedules/auto-plan", new AutoPlanScheduleRequest(
            classId, term, new List<DayOfWeek> { DayOfWeek.Saturday }, new TimeOnly(8, 0), 4, 60,
            new List<ScheduleRequirementInput>
            {
                new(courses![0].Id, teachers![0].Id, 2),
                new(courses[1].Id, teachers[1].Id, 2),
            }));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AutoPlanScheduleResultDto>();

        Assert.True(result!.FullyPlaced);
        Assert.Empty(result.Unplaced);
        Assert.Equal(4, result.Proposed.Count);

        var teacherSlots = result.Proposed.Select(p => (p.TeacherId, p.DayOfWeek, p.StartTime)).ToList();
        Assert.Equal(teacherSlots.Count, teacherSlots.Distinct().Count());

        var roomSlots = result.Proposed.Select(p => (p.RoomId, p.DayOfWeek, p.StartTime)).ToList();
        Assert.Equal(roomSlots.Count, roomSlots.Distinct().Count());

        var classSlots = result.Proposed.Select(p => (p.DayOfWeek, p.StartTime)).ToList();
        Assert.Equal(classSlots.Count, classSlots.Distinct().Count());
    }

    [Fact]
    public async Task Director_AutoPlan_ReportsUnplacedSessions_WhenNotEnoughSlots()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var term = existing!.First().AcademicTermId;
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        // Un seul créneau disponible (1 jour × 1 période) pour 2 séances demandées : la seconde ne peut pas être placée.
        var response = await client.PostAsJsonAsync("/api/courseschedules/auto-plan", new AutoPlanScheduleRequest(
            classId, term, new List<DayOfWeek> { DayOfWeek.Saturday }, new TimeOnly(13, 0), 1, 60,
            new List<ScheduleRequirementInput> { new(courses![0].Id, teachers![0].Id, 2) }));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AutoPlanScheduleResultDto>();

        Assert.False(result!.FullyPlaced);
        Assert.Single(result.Proposed);
        Assert.Single(result.Unplaced);
    }

    [Fact]
    public async Task Director_CanCommitProposal_AndSchedulesArePersisted()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var countBefore = existing!.Count;
        var term = existing.First().AcademicTermId;
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var planResponse = await client.PostAsJsonAsync("/api/courseschedules/auto-plan", new AutoPlanScheduleRequest(
            classId, term, new List<DayOfWeek> { DayOfWeek.Saturday }, new TimeOnly(15, 0), 2, 45,
            new List<ScheduleRequirementInput> { new(courses![2].Id, teachers![0].Id, 1) }));
        var plan = await planResponse.Content.ReadFromJsonAsync<AutoPlanScheduleResultDto>();
        Assert.Single(plan!.Proposed);

        var commitResponse = await client.PostAsJsonAsync("/api/courseschedules/auto-plan/commit",
            new CommitAutoPlanRequest(plan.Proposed));
        commitResponse.EnsureSuccessStatusCode();
        var commitResult = await commitResponse.Content.ReadFromJsonAsync<CommitAutoPlanResultDto>();

        Assert.Single(commitResult!.Created);
        Assert.Empty(commitResult.Skipped);

        var afterCount = (await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules"))!.Count;
        Assert.Equal(countBefore + 1, afterCount);
    }

    [Fact]
    public async Task CommitProposal_SkipsSlot_IfRoomWasReservedInTheMeantime()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var term = existing!.First().AcademicTermId;
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var planResponse = await client.PostAsJsonAsync("/api/courseschedules/auto-plan", new AutoPlanScheduleRequest(
            classId, term, new List<DayOfWeek> { DayOfWeek.Saturday }, new TimeOnly(17, 0), 1, 60,
            new List<ScheduleRequirementInput> { new(courses![3].Id, teachers![1].Id, 1) }));
        var plan = await planResponse.Content.ReadFromJsonAsync<AutoPlanScheduleResultDto>();
        var slot = Assert.Single(plan!.Proposed);

        // Quelqu'un d'autre réserve la même salle sur le même créneau avant que la proposition soit confirmée.
        var otherTeacher = teachers!.First(t => t.Id != slot.TeacherId);
        var interveningResponse = await client.PostAsJsonAsync("/api/courseschedules", new CreateCourseScheduleRequest(
            courses![4].Id, slot.RoomId, otherTeacher.Id, null, term, slot.DayOfWeek, slot.StartTime, slot.EndTime));
        interveningResponse.EnsureSuccessStatusCode();

        var commitResponse = await client.PostAsJsonAsync("/api/courseschedules/auto-plan/commit",
            new CommitAutoPlanRequest(plan.Proposed));
        var commitResult = await commitResponse.Content.ReadFromJsonAsync<CommitAutoPlanResultDto>();

        Assert.Empty(commitResult!.Created);
        Assert.Single(commitResult.Skipped);
    }

    [Fact]
    public async Task Teacher_CannotUseAutoPlan()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var existing = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        var response = await client.PostAsJsonAsync("/api/courseschedules/auto-plan", new AutoPlanScheduleRequest(
            students!.First().ClassId, existing!.First().AcademicTermId, new List<DayOfWeek> { DayOfWeek.Saturday },
            new TimeOnly(8, 0), 1, 60,
            new List<ScheduleRequirementInput> { new(courses![0].Id, teachers![0].Id, 1) }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
