using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.CourseEnrollments;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseEnrollmentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public CourseEnrollmentsController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<CourseEnrollmentDto>>> GetAll([FromQuery] Guid? courseId)
    {
        var query = BaseQuery();

        if (courseId.HasValue)
            query = query.Where(e => e.CourseId == courseId.Value);

        var enrollments = await query.OrderBy(e => e.Student.LastName).ToListAsync();

        return Ok(enrollments.Select(ToDto));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<CourseEnrollmentDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        // Le Parent (sans claim école) accède aux inscriptions de son propre enfant, déjà vérifié ci-dessus.
        var enrollments = await BaseQuery().IgnoreQueryFilters()
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Course.Name)
            .ToListAsync();

        return Ok(enrollments.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CourseEnrollmentDto>> Create(CreateCourseEnrollmentRequest request)
    {
        var student = await _context.Students.FindAsync(request.StudentId);
        var course = await _context.Courses.FindAsync(request.CourseId);
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);

        if (student is null || course is null || year is null)
            return NotFound(new { message = "Élève, cours ou année académique introuvable." });

        if (!await HasProgramEnrollmentAsync(request.StudentId, course.ProgramId, request.AcademicYearId))
            return BadRequest(new { message = "Cet élève doit d'abord être inscrit au programme de ce cours pour cette année." });

        var exists = await _context.CourseEnrollments.AnyAsync(e =>
            e.StudentId == request.StudentId && e.CourseId == request.CourseId && e.AcademicYearId == request.AcademicYearId);

        if (exists)
            return Conflict(new { message = "Cet élève est déjà inscrit à ce cours pour cette année." });

        var enrollment = new CourseEnrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            AcademicYearId = request.AcademicYearId,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.CourseEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(e => e.Id == enrollment.Id);
        return Ok(ToDto(full));
    }

    /// Outil d'inscription en masse : rattache une liste d'élèves à un cours en une seule requête.
    /// Seuls les élèves déjà inscrits au programme de ce cours (pour la même année) sont retenus ;
    /// les autres sont silencieusement ignorés, à l'image de l'outil équivalent pour les programmes.
    [HttpPost("bulk")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<CourseEnrollmentDto>>> BulkEnroll(BulkCourseEnrollRequest request)
    {
        var course = await _context.Courses.FindAsync(request.CourseId);
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (course is null || year is null)
            return NotFound(new { message = "Cours ou année académique introuvable." });

        var existingStudentIds = await _context.CourseEnrollments
            .Where(e => e.CourseId == request.CourseId && e.AcademicYearId == request.AcademicYearId)
            .Select(e => e.StudentId)
            .ToListAsync();

        var eligibleStudentIds = await _context.ProgramEnrollments
            .Where(e => e.ProgramId == course.ProgramId && e.AcademicYearId == request.AcademicYearId &&
                        e.Status == EnrollmentStatus.Active && request.StudentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToListAsync();

        var studentIdsToAdd = eligibleStudentIds.Distinct().Except(existingStudentIds).ToList();

        foreach (var studentId in studentIdsToAdd)
        {
            _context.CourseEnrollments.Add(new CourseEnrollment
            {
                StudentId = studentId,
                CourseId = request.CourseId,
                AcademicYearId = request.AcademicYearId,
                SchoolId = _currentUser.SchoolId!.Value
            });
        }

        await _context.SaveChangesAsync();

        var enrollments = await BaseQuery()
            .Where(e => e.CourseId == request.CourseId && e.AcademicYearId == request.AcademicYearId)
            .ToListAsync();

        return Ok(enrollments.Select(ToDto));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CourseEnrollmentDto>> UpdateStatus(Guid id, [FromBody] EnrollmentStatus status)
    {
        var enrollment = await _context.CourseEnrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        enrollment.Status = status;
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(e => e.Id == id);
        return Ok(ToDto(full));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var enrollment = await _context.CourseEnrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        _context.CourseEnrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasProgramEnrollmentAsync(Guid studentId, Guid programId, Guid academicYearId) =>
        await _context.ProgramEnrollments.AnyAsync(e =>
            e.StudentId == studentId && e.ProgramId == programId && e.AcademicYearId == academicYearId &&
            e.Status == EnrollmentStatus.Active);

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private IQueryable<CourseEnrollment> BaseQuery() => _context.CourseEnrollments
        .Include(e => e.Student)
        .Include(e => e.Course)
        .Include(e => e.AcademicYear);

    private static CourseEnrollmentDto ToDto(CourseEnrollment e) => new(
        e.Id, e.StudentId, e.Student.FullName,
        e.CourseId, e.Course.Name,
        e.AcademicYearId, e.AcademicYear.Name,
        e.EnrollmentDate, e.Status.ToString());
}
