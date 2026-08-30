using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.FinalGrades;
using GestionScolaire.Application.Services;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinalGradesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public FinalGradesController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet("class/{classId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<FinalGradeDto>>> GetByClass(Guid classId, [FromQuery] string term)
    {
        if (!await CanAccessClassAsync(classId)) return Forbid();

        var students = await _context.Students
            .Where(s => s.ClassId == classId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var grades = await _context.Grades
            .Include(g => g.Subject)
            .Where(g => g.ClassId == classId && g.Term == term)
            .ToListAsync();

        var defaultScale = await _context.GradingScales.Include(s => s.Intervals).FirstOrDefaultAsync(s => s.IsDefault);

        var averages = students
            .Select(s => (Student: s, Result: GradeAverageCalculator.CalculateGeneralAverage(s.Id, s.FullName, grades.Where(g => g.StudentId == s.Id))))
            .OrderByDescending(x => x.Result.GeneralAverage)
            .ToList();

        var result = averages.Select((x, index) => new FinalGradeDto(
            x.Student.Id, x.Student.FullName, x.Result.GeneralAverage,
            GradeAverageCalculator.GetMention(x.Result.GeneralAverage),
            FindLetterGrade(defaultScale, x.Result.GeneralAverage),
            index + 1, averages.Count, x.Result.SubjectAverages)).ToList();

        return Ok(result);
    }

    /// Rapport d'évaluation par cours : moyenne, min, max et nombre d'élèves évalués, pour chaque matière de la classe.
    [HttpGet("class/{classId:guid}/by-course")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<CourseWiseAssessmentDto>>> GetByCourse(Guid classId, [FromQuery] string term)
    {
        if (!await CanAccessClassAsync(classId)) return Forbid();

        var students = await _context.Students
            .Where(s => s.ClassId == classId && s.IsActive)
            .ToListAsync();

        var grades = await _context.Grades
            .Include(g => g.Subject)
            .Where(g => g.ClassId == classId && g.Term == term)
            .ToListAsync();

        var studentSubjectAverages = students
            .SelectMany(s => GradeAverageCalculator.CalculateGeneralAverage(s.Id, s.FullName, grades.Where(g => g.StudentId == s.Id)).SubjectAverages)
            .ToList();

        var report = studentSubjectAverages
            .GroupBy(sa => sa.SubjectName)
            .Select(g => new CourseWiseAssessmentDto(
                g.Key,
                Math.Round(g.Average(x => x.Average), 2),
                g.Min(x => x.Average),
                g.Max(x => x.Average),
                g.Count()))
            .OrderBy(x => x.CourseName)
            .ToList();

        return Ok(report);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<FinalGradeDto>> GetByStudent(Guid studentId, [FromQuery] string term)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        // Un Parent (déjà vérifié ci-dessus via l'access policy) n'a pas de claim école. Pour tout autre
        // rôle les filtres restent actifs : HasAccessAsync ne vérifie pas l'école pour un Directeur, ce
        // sont les filtres Student/Grade/GradingScale qui referment la frontière multi-tenant ici.
        var isParent = _currentUser.Role == nameof(UserRole.Parent);

        var studentQuery = _context.Students.Where(s => s.Id == studentId);
        if (isParent) studentQuery = studentQuery.IgnoreQueryFilters();
        var student = await studentQuery.FirstOrDefaultAsync();
        if (student is null) return NotFound();

        var classmatesQuery = _context.Students.Where(s => s.ClassId == student.ClassId && s.IsActive);
        if (isParent) classmatesQuery = classmatesQuery.IgnoreQueryFilters();
        var classmates = await classmatesQuery.ToListAsync();

        var gradesQuery = _context.Grades.Include(g => g.Subject).Where(g => g.ClassId == student.ClassId && g.Term == term);
        if (isParent) gradesQuery = gradesQuery.IgnoreQueryFilters();
        var grades = await gradesQuery.ToListAsync();

        var scaleQuery = _context.GradingScales.Include(s => s.Intervals).Where(s => s.IsDefault);
        if (isParent) scaleQuery = scaleQuery.IgnoreQueryFilters();
        var defaultScale = await scaleQuery.FirstOrDefaultAsync();

        var averages = classmates
            .Select(s => (StudentId: s.Id, Average: GradeAverageCalculator.CalculateGeneralAverage(s.Id, s.FullName, grades.Where(g => g.StudentId == s.Id))))
            .OrderByDescending(x => x.Average.GeneralAverage)
            .ToList();

        var rank = averages.FindIndex(x => x.StudentId == studentId) + 1;
        var own = averages.First(x => x.StudentId == studentId).Average;

        return Ok(new FinalGradeDto(
            studentId, own.StudentFullName, own.GeneralAverage,
            GradeAverageCalculator.GetMention(own.GeneralAverage),
            FindLetterGrade(defaultScale, own.GeneralAverage),
            rank, averages.Count, own.SubjectAverages));
    }

    private static string? FindLetterGrade(GradingScale? scale, decimal average) =>
        scale?.Intervals.FirstOrDefault(i => average >= i.MinScore && average <= i.MaxScore)?.Grade;

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private async Task<bool> CanAccessClassAsync(Guid classId)
    {
        if (_currentUser.Role != nameof(UserRole.Teacher)) return true;

        return await _context.Classes.IgnoreQueryFilters().AnyAsync(c =>
            c.Id == classId && c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId);
    }
}
