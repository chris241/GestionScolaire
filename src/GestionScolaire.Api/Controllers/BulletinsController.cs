using System.IO.Compression;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Grades;
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
            .Include(s => s.Class).ThenInclude(c => c.AcademicYear)
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

        var (pdfBytes, fileName) = BuildBulletin(student, term, classmateIds, termGrades);

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// Génération groupée : un ZIP contenant le bulletin PDF de chaque élève actif de la classe.
    [HttpGet("class/{classId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<IActionResult> GenerateClassBulletins(Guid classId, [FromQuery] string term)
    {
        if (!await CanAccessClassAsync(classId)) return Forbid();

        var schoolClass = await _context.Classes
            .Include(c => c.AcademicYear)
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (schoolClass is null) return NotFound(new { message = "Classe introuvable." });

        var students = await _context.Students
            .Where(s => s.ClassId == classId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        if (students.Count == 0) return NotFound(new { message = "Aucun élève actif dans cette classe." });

        var studentIds = students.Select(s => s.Id).ToList();
        var termGrades = await _context.Grades
            .Include(g => g.Subject)
            .Where(g => studentIds.Contains(g.StudentId) && g.Term == term)
            .ToListAsync();

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var student in students)
            {
                student.Class = schoolClass;
                var (pdfBytes, fileName) = BuildBulletin(student, term, studentIds, termGrades);

                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                entryStream.Write(pdfBytes);
            }
        }

        var zipFileName = $"bulletins_{schoolClass.Name}_{term}.zip".Replace(" ", "_");
        return File(zipStream.ToArray(), "application/zip", zipFileName);
    }

    private (byte[] PdfBytes, string FileName) BuildBulletin(Student student, string term, List<Guid> classmateIds, List<Grade> termGrades)
    {
        var classAverages = classmateIds
            .Select(id =>
            {
                var studentGrades = termGrades.Where(g => g.StudentId == id);
                var average = GradeAverageCalculator.CalculateGeneralAverage(id, string.Empty, studentGrades).GeneralAverage;
                return (StudentId: id, Average: average);
            })
            .OrderByDescending(x => x.Average)
            .ToList();

        var rank = classAverages.FindIndex(x => x.StudentId == student.Id) + 1;

        var studentGeneral = GradeAverageCalculator.CalculateGeneralAverage(
            student.Id, student.FullName, termGrades.Where(g => g.StudentId == student.Id));

        var bulletin = new BulletinDto
        {
            StudentFullName = student.FullName,
            EnrollmentNumber = student.EnrollmentNumber,
            ClassName = student.Class.Name,
            AcademicYear = student.Class.AcademicYear.Name,
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

        return (pdfBytes, fileName);
    }

    private async Task<bool> CanAccessClassAsync(Guid classId)
    {
        if (_currentUser.Role != nameof(UserRole.Teacher)) return true;

        return await _context.Classes.AnyAsync(c =>
            c.Id == classId && c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId);
    }
}
