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

        var student = await _context.Students.FindAsync(studentId);
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
        var teacher = await _context.Teachers.FindAsync(request.TeacherId);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);

        if (course is null || room is null || teacher is null || term is null)
            return NotFound(new { message = "Cours, salle, enseignant ou trimestre introuvable." });

        if (request.ClassId.HasValue && await _context.Classes.FindAsync(request.ClassId.Value) is null)
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

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private IQueryable<CourseSchedule> BaseQuery() => _context.CourseSchedules
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
