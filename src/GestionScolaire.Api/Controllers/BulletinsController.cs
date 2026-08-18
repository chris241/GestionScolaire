using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BulletinsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IBulletinPdfService _pdfService;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public BulletinsController(
        IApplicationDbContext context,
        IBulletinPdfService pdfService,
        ICurrentUserService currentUser,
        IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _pdfService = pdfService;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> GenerateBulletin(Guid studentId, [FromQuery] string term)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId))
            return Forbid();

        var student = await _context.Students
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student is null) return NotFound(new { message = "Élève introuvable." });

        var classmateIds = await _context.Students
            .Where(s => s.ClassId == student.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        var termGrades = await _context.Grades
            .Include(g => g.Subject)
            .Where(g => classmateIds.Contains(g.StudentId) && g.Term == term)
            .ToListAsync();

        var classAverages = classmateIds
            .Select(id =>
            {
                var studentGrades = termGrades.Where(g => g.StudentId == id);
                var average = GradeAverageCalculator.CalculateGeneralAverage(id, string.Empty, studentGrades).GeneralAverage;
                return (StudentId: id, Average: average);
            })
            .OrderByDescending(x => x.Average)
            .ToList();

        var rank = classAverages.FindIndex(x => x.StudentId == studentId) + 1;

        var studentGeneral = GradeAverageCalculator.CalculateGeneralAverage(
            studentId, student.FullName, termGrades.Where(g => g.StudentId == studentId));

        var bulletin = new BulletinDto
        {
            StudentFullName = student.FullName,
            EnrollmentNumber = student.EnrollmentNumber,
            ClassName = student.Class.Name,
            AcademicYear = student.Class.AcademicYear,
            Term = term,
            GeneralAverage = studentGeneral.GeneralAverage,
            ClassRank = rank,
            ClassSize = classAverages.Count,
            Mention = GradeAverageCalculator.GetMention(studentGeneral.GeneralAverage),
            Subjects = studentGeneral.SubjectAverages.Select(s => new BulletinSubjectLineDto
            {
                SubjectName = s.SubjectName,
                Average = s.Average,
                Coefficient = termGrades.FirstOrDefault(g => g.Subject.Name == s.SubjectName)?.Subject.Coefficient ?? 1
            }).ToList()
        };

        var pdfBytes = _pdfService.GenerateBulletin(bulletin);
        var fileName = $"bulletin_{student.LastName}_{student.FirstName}_{term}.pdf".Replace(" ", "_");

        return File(pdfBytes, "application/pdf", fileName);
    }
}
