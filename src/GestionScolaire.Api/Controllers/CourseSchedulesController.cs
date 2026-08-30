using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.CourseSchedules;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseSchedulesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public CourseSchedulesController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseScheduleDto>>> GetAll([FromQuery] Guid? classId, [FromQuery] Guid? academicTermId)
    {
        var query = BaseQuery();

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        if (academicTermId.HasValue)
            query = query.Where(s => s.AcademicTermId == academicTermId.Value);

        var schedules = await query
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(schedules.Select(ToDto));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<CourseScheduleDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var student = await _context.Students.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == studentId);
        if (student is null) return NotFound();

        var schedules = await BaseQuery()
            .Where(s => s.ClassId == student.ClassId)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToListAsync();

        return Ok(schedules.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CourseScheduleDto>> Create(CreateCourseScheduleRequest request)
    {
        var course = await _context.Courses.FindAsync(request.CourseId);
        var room = await _context.Rooms.FindAsync(request.RoomId);
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == request.TeacherId);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);

        if (course is null || room is null || teacher is null || term is null)
            return NotFound(new { message = "Cours, salle, enseignant ou trimestre introuvable." });

        if (request.ClassId.HasValue && await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId.Value) is null)
            return NotFound(new { message = "Classe introuvable." });

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { message = "L'heure de fin doit être postérieure à l'heure de début." });

        var conflict = await _context.CourseSchedules.AnyAsync(s =>
            s.RoomId == request.RoomId &&
            s.AcademicTermId == request.AcademicTermId &&
            s.DayOfWeek == request.DayOfWeek &&
            s.StartTime == request.StartTime);

        if (conflict)
            return Conflict(new { message = "Cette salle est déjà réservée sur ce créneau." });

        var schedule = new CourseSchedule
        {
            CourseId = request.CourseId,
            RoomId = request.RoomId,
            TeacherId = request.TeacherId,
            ClassId = request.ClassId,
            AcademicTermId = request.AcademicTermId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _context.CourseSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(s => s.Id == schedule.Id);
        return Ok(ToDto(full));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var schedule = await _context.CourseSchedules.FindAsync(id);
        if (schedule is null) return NotFound();

        _context.CourseSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Assistant de planification : propose un placement complet (sans l'enregistrer) pour une classe et un
    /// trimestre donnés, à partir d'une liste de cours à placer (avec enseignant et nombre de séances/semaine).
    /// Algorithme glouton : pour chaque cours, essaie d'étaler les séances sur des jours différents, en
    /// choisissant le premier créneau où l'enseignant, la classe et une salle sont tous les trois libres —
    /// en tenant compte à la fois des créneaux déjà enregistrés en base et de ceux déjà proposés dans ce même appel.
    [HttpPost("auto-plan")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AutoPlanScheduleResultDto>> AutoPlan(AutoPlanScheduleRequest request)
    {
        var schoolClass = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);
        if (schoolClass is null || term is null)
            return NotFound(new { message = "Classe ou trimestre introuvable." });

        var courseIds = request.Requirements.Select(r => r.CourseId).Distinct().ToList();
        var courses = await _context.Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        if (courseIds.Any(id => !courses.ContainsKey(id)))
            return NotFound(new { message = "Un ou plusieurs cours sont introuvables." });

        var teacherIds = request.Requirements.Select(r => r.TeacherId).Distinct().ToList();
        var teachers = await _context.Teachers.Include(t => t.User).Where(t => teacherIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        if (teacherIds.Any(id => !teachers.ContainsKey(id)))
            return NotFound(new { message = "Un ou plusieurs enseignants sont introuvables." });

        var rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        if (rooms.Count == 0)
            return BadRequest(new { message = "Aucune salle n'est définie ; impossible de proposer un planning." });

        var slots = new List<(DayOfWeek Day, TimeOnly Start, TimeOnly End)>();
        foreach (var day in request.Days.Distinct())
        {
            var start = request.DailyStartTime;
            for (var p = 0; p < request.PeriodsPerDay; p++)
            {
                var end = start.AddMinutes(request.PeriodDurationMinutes);
                slots.Add((day, start, end));
                start = end;
            }
        }

        var existing = await _context.CourseSchedules
            .Where(s => s.AcademicTermId == request.AcademicTermId)
            .Select(s => new { s.TeacherId, s.RoomId, s.ClassId, s.DayOfWeek, s.StartTime })
            .ToListAsync();

        var teacherBusy = new HashSet<(Guid, DayOfWeek, TimeOnly)>(existing.Select(s => (s.TeacherId, s.DayOfWeek, s.StartTime)));
        var roomBusy = new HashSet<(Guid, DayOfWeek, TimeOnly)>(existing.Select(s => (s.RoomId, s.DayOfWeek, s.StartTime)));
        var classBusy = new HashSet<(DayOfWeek, TimeOnly)>(existing.Where(s => s.ClassId == request.ClassId).Select(s => (s.DayOfWeek, s.StartTime)));

        var proposed = new List<ProposedScheduleSlot>();
        var unplaced = new List<string>();

        foreach (var req in request.Requirements)
        {
            var course = courses[req.CourseId];
            var teacher = teachers[req.TeacherId];
            var teacherName = $"{teacher.User.FirstName} {teacher.User.LastName}";
            var usedDaysForCourse = new HashSet<DayOfWeek>();

            for (var session = 1; session <= req.SessionsPerWeek; session++)
            {
                var ordered = slots.OrderBy(s => usedDaysForCourse.Contains(s.Day) ? 1 : 0).ThenBy(s => s.Day).ThenBy(s => s.Start);

                (DayOfWeek Day, TimeOnly Start, TimeOnly End)? chosen = null;
                Room? chosenRoom = null;

                foreach (var slot in ordered)
                {
                    if (teacherBusy.Contains((req.TeacherId, slot.Day, slot.Start))) continue;
                    if (classBusy.Contains((slot.Day, slot.Start))) continue;

                    var room = rooms.FirstOrDefault(r => !roomBusy.Contains((r.Id, slot.Day, slot.Start)));
                    if (room is null) continue;

                    chosen = slot;
                    chosenRoom = room;
                    break;
                }

                if (chosen is null || chosenRoom is null)
                {
                    unplaced.Add($"{course.Name} : séance {session}/{req.SessionsPerWeek} non placée (aucun créneau libre pour {teacherName}).");
                    continue;
                }

                teacherBusy.Add((req.TeacherId, chosen.Value.Day, chosen.Value.Start));
                roomBusy.Add((chosenRoom.Id, chosen.Value.Day, chosen.Value.Start));
                classBusy.Add((chosen.Value.Day, chosen.Value.Start));
                usedDaysForCourse.Add(chosen.Value.Day);

                proposed.Add(new ProposedScheduleSlot(
                    course.Id, course.Name,
                    teacher.Id, teacherName,
                    chosenRoom.Id, chosenRoom.Name,
                    request.ClassId, request.AcademicTermId,
                    chosen.Value.Day, chosen.Value.Start, chosen.Value.End));
            }
        }

        return Ok(new AutoPlanScheduleResultDto(unplaced.Count == 0, proposed, unplaced));
    }

    /// Enregistre une proposition (éventuellement modifiée par le Directeur) issue de l'assistant de planification.
    /// Revalide chaque créneau au moment de l'écriture (la salle a pu être réservée entre-temps par ailleurs).
    [HttpPost("auto-plan/commit")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CommitAutoPlanResultDto>> CommitAutoPlan(CommitAutoPlanRequest request)
    {
        var courseIds = request.Slots.Select(s => s.CourseId).Distinct().ToList();
        var roomIds = request.Slots.Select(s => s.RoomId).Distinct().ToList();
        var teacherIds = request.Slots.Select(s => s.TeacherId).Distinct().ToList();
        var classIds = request.Slots.Select(s => s.ClassId).Distinct().ToList();
        var termIds = request.Slots.Select(s => s.AcademicTermId).Distinct().ToList();

        var validCourseIds = (await _context.Courses.Where(c => courseIds.Contains(c.Id)).Select(c => c.Id).ToListAsync()).ToHashSet();
        var validRoomIds = (await _context.Rooms.Where(r => roomIds.Contains(r.Id)).Select(r => r.Id).ToListAsync()).ToHashSet();
        var validTeacherIds = (await _context.Teachers.Where(t => teacherIds.Contains(t.Id)).Select(t => t.Id).ToListAsync()).ToHashSet();
        var validClassIds = (await _context.Classes.Where(c => classIds.Contains(c.Id)).Select(c => c.Id).ToListAsync()).ToHashSet();
        var validTermIds = (await _context.AcademicTerms.Where(t => termIds.Contains(t.Id)).Select(t => t.Id).ToListAsync()).ToHashSet();

        var localKeys = new HashSet<(Guid RoomId, Guid TermId, DayOfWeek Day, TimeOnly Start)>();
        var createdIds = new List<Guid>();
        var skipped = new List<string>();

        foreach (var slot in request.Slots)
        {
            var label = $"{slot.CourseName} ({slot.DayOfWeek} {slot.StartTime})";

            if (!validCourseIds.Contains(slot.CourseId) || !validRoomIds.Contains(slot.RoomId) ||
                !validTeacherIds.Contains(slot.TeacherId) || !validClassIds.Contains(slot.ClassId) ||
                !validTermIds.Contains(slot.AcademicTermId))
            {
                skipped.Add($"{label} : référence invalide (cours, salle, enseignant, classe ou trimestre).");
                continue;
            }

            if (slot.EndTime <= slot.StartTime)
            {
                skipped.Add($"{label} : heure de fin invalide.");
                continue;
            }

            var key = (slot.RoomId, slot.AcademicTermId, slot.DayOfWeek, slot.StartTime);
            if (!localKeys.Add(key))
            {
                skipped.Add($"{label} : conflit de salle au sein de cette proposition.");
                continue;
            }

            var conflict = await _context.CourseSchedules.AnyAsync(s =>
                s.RoomId == slot.RoomId && s.AcademicTermId == slot.AcademicTermId &&
                s.DayOfWeek == slot.DayOfWeek && s.StartTime == slot.StartTime);

            if (conflict)
            {
                skipped.Add($"{label} : cette salle a été réservée entre-temps.");
                continue;
            }

            var schedule = new CourseSchedule
            {
                CourseId = slot.CourseId,
                RoomId = slot.RoomId,
                TeacherId = slot.TeacherId,
                ClassId = slot.ClassId,
                AcademicTermId = slot.AcademicTermId,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime
            };

            _context.CourseSchedules.Add(schedule);
            createdIds.Add(schedule.Id);
        }

        await _context.SaveChangesAsync();

        var created = await BaseQuery().Where(s => createdIds.Contains(s.Id)).ToListAsync();

        return Ok(new CommitAutoPlanResultDto(created.Select(ToDto).ToList(), skipped));
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private IQueryable<CourseSchedule> BaseQuery() => _context.CourseSchedules.IgnoreQueryFilters()
        .Include(s => s.Course)
        .Include(s => s.Room)
        .Include(s => s.Teacher).ThenInclude(t => t.User)
        .Include(s => s.Class)
        .Include(s => s.AcademicTerm);

    private static CourseScheduleDto ToDto(CourseSchedule s) => new(
        s.Id, s.CourseId, s.Course.Name,
        s.RoomId, s.Room.Name,
        s.TeacherId, $"{s.Teacher.User.FirstName} {s.Teacher.User.LastName}",
        s.ClassId, s.Class?.Name,
        s.AcademicTermId, s.AcademicTerm.Name,
        s.DayOfWeek, s.StartTime, s.EndTime);
}
