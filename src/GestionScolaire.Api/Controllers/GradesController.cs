using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Application.Services;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public GradesController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<ActionResult<GradeDto>> Create(CreateGradeRequest request)
    {
        var isDirector = User.IsInRole("Director");
        var teacherId = isDirector ? request.TeacherId : await ResolveTeacherIdAsync();

        if (teacherId is null)
            return isDirector
                ? BadRequest(new { message = "TeacherId requis lorsque la saisie est faite par un directeur." })
                : Forbid();

        var student = await _context.Students.FindAsync(request.StudentId);
        var subject = await _context.Subjects.FindAsync(request.SubjectId);
        if (student is null || subject is null)
            return NotFound(new { message = "Élève ou matière introuvable." });

        if (request.AssessmentPlanId.HasValue && await _context.AssessmentPlans.FindAsync(request.AssessmentPlanId.Value) is null)
            return NotFound(new { message = "Plan d'évaluation introuvable." });

        if (!await HasAccessAsync(request.StudentId)) return Forbid();

        var grade = new Grade
        {
            StudentId = request.StudentId,
            SubjectId = request.SubjectId,
            ClassId = request.ClassId,
            TeacherId = teacherId.Value,
            Score = request.Score,
            MaxScore = request.MaxScore,
            Coefficient = request.Coefficient,
            Type = request.Type,
            Term = request.Term,
            Comment = request.Comment,
            AssessmentPlanId = request.AssessmentPlanId
        };

        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();

        return Ok(ToDto(grade, student.FullName, subject.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<ActionResult<GradeDto>> Update(Guid id, UpdateGradeRequest request)
    {
        var grade = await _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grade is null) return NotFound();
        if (!await HasAccessAsync(grade.StudentId)) return Forbid();

        grade.Score = request.Score;
        grade.MaxScore = request.MaxScore;
        grade.Coefficient = request.Coefficient;
        grade.Comment = request.Comment;

        await _context.SaveChangesAsync();

        return Ok(ToDto(grade, grade.Student.FullName, grade.Subject.Name));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade is null) return NotFound();
        if (!await HasAccessAsync(grade.StudentId)) return Forbid();

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<GradeDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        // Peut être appelé par un Parent (sans claim école), déjà vérifié ci-dessus via l'access policy.
        var grades = await _context.Grades.IgnoreQueryFilters()
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.EvaluatedAt)
            .ToListAsync();

        return Ok(grades.Select(g => ToDto(g, g.Student.FullName, g.Subject.Name)));
    }

    [HttpGet("student/{studentId:guid}/average")]
    public async Task<ActionResult<StudentGeneralAverageDto>> GetStudentAverage(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var student = await _context.Students.FindAsync(studentId);
        if (student is null) return NotFound();

        var grades = await _context.Grades.IgnoreQueryFilters()
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId)
            .ToListAsync();

        var result = GradeAverageCalculator.CalculateGeneralAverage(studentId, student.FullName, grades);
        return Ok(result);
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private async Task<Guid?> ResolveTeacherIdAsync()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var teacher = await _context.Teachers.IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.User.Email == email);

        return teacher?.Id;
    }

    private static GradeDto ToDto(Grade g, string studentName, string subjectName) => new(
        g.Id, g.StudentId, studentName, g.SubjectId, subjectName,
        g.Score, g.MaxScore, g.Coefficient, g.Type.ToString(), g.Term, g.EvaluatedAt, g.Comment, g.AssessmentPlanId);
}
