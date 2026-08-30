using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.ProgramEnrollments;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgramEnrollmentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProgramEnrollmentsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<ProgramEnrollmentDto>>> GetAll([FromQuery] Guid? programId)
    {
        var query = BaseQuery();

        if (programId.HasValue)
            query = query.Where(e => e.ProgramId == programId.Value);

        var enrollments = await query.OrderBy(e => e.Student.LastName).ToListAsync();

        return Ok(enrollments.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<ProgramEnrollmentDto>> Create(CreateProgramEnrollmentRequest request)
    {
        var student = await _context.Students.FindAsync(request.StudentId);
        var program = await _context.AcademicPrograms.FindAsync(request.ProgramId);
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);

        if (student is null || program is null || year is null)
            return NotFound(new { message = "Élève, programme ou année académique introuvable." });

        var exists = await _context.ProgramEnrollments.AnyAsync(e =>
            e.StudentId == request.StudentId && e.ProgramId == request.ProgramId && e.AcademicYearId == request.AcademicYearId);

        if (exists)
            return Conflict(new { message = "Cet élève est déjà inscrit à ce programme pour cette année." });

        var enrollment = new ProgramEnrollment
        {
            StudentId = request.StudentId,
            ProgramId = request.ProgramId,
            AcademicYearId = request.AcademicYearId,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.ProgramEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(e => e.Id == enrollment.Id);
        return Ok(ToDto(full));
    }

    /// Outil d'inscription en masse : rattache une liste d'élèves à un programme en une seule requête.
    [HttpPost("bulk")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<ProgramEnrollmentDto>>> BulkEnroll(BulkEnrollRequest request)
    {
        var program = await _context.AcademicPrograms.FindAsync(request.ProgramId);
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (program is null || year is null)
            return NotFound(new { message = "Programme ou année académique introuvable." });

        var existingStudentIds = await _context.ProgramEnrollments
            .Where(e => e.ProgramId == request.ProgramId && e.AcademicYearId == request.AcademicYearId)
            .Select(e => e.StudentId)
            .ToListAsync();

        var studentIdsToAdd = request.StudentIds.Distinct().Except(existingStudentIds).ToList();

        var validStudentIds = await _context.Students
            .Where(s => studentIdsToAdd.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var studentId in validStudentIds)
        {
            _context.ProgramEnrollments.Add(new ProgramEnrollment
            {
                StudentId = studentId,
                ProgramId = request.ProgramId,
                AcademicYearId = request.AcademicYearId,
                SchoolId = _currentUser.SchoolId!.Value
            });
        }

        await _context.SaveChangesAsync();

        var enrollments = await BaseQuery()
            .Where(e => e.ProgramId == request.ProgramId && e.AcademicYearId == request.AcademicYearId)
            .ToListAsync();

        return Ok(enrollments.Select(ToDto));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<ProgramEnrollmentDto>> UpdateStatus(Guid id, [FromBody] EnrollmentStatus status)
    {
        var enrollment = await _context.ProgramEnrollments.FindAsync(id);
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
        var enrollment = await _context.ProgramEnrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        _context.ProgramEnrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<ProgramEnrollment> BaseQuery() => _context.ProgramEnrollments
        .Include(e => e.Student)
        .Include(e => e.Program)
        .Include(e => e.AcademicYear);

    private static ProgramEnrollmentDto ToDto(ProgramEnrollment e) => new(
        e.Id, e.StudentId, e.Student.FullName,
        e.ProgramId, e.Program.Name,
        e.AcademicYearId, e.AcademicYear.Name,
        e.EnrollmentDate, e.Status.ToString());
}
