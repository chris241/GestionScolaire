using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Director")]
public class StudentApplicantsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public StudentApplicantsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentApplicantDto>>> GetAll([FromQuery] AdmissionStatus? status)
    {
        var query = _context.StudentApplicants.Include(a => a.AcademicYear).AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var applicants = await query
            .OrderByDescending(a => a.AppliedDate)
            .ToListAsync();

        return Ok(applicants.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentApplicantDto>> GetById(Guid id)
    {
        var applicant = await _context.StudentApplicants.Include(a => a.AcademicYear).FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        return Ok(ToDto(applicant));
    }

    [HttpPost]
    public async Task<ActionResult<StudentApplicantDto>> Create(CreateStudentApplicantRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        var applicant = new StudentApplicant
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth.AsUtc(),
            Gender = request.Gender,
            Email = request.Email,
            Phone = request.Phone,
            GuardianName = request.GuardianName,
            GuardianEmail = request.GuardianEmail,
            GuardianPhone = request.GuardianPhone,
            LevelAppliedFor = request.LevelAppliedFor,
            AcademicYearId = request.AcademicYearId,
            Status = AdmissionStatus.Submitted
        };

        _context.StudentApplicants.Add(applicant);
        await _context.SaveChangesAsync();

        applicant.AcademicYear = year;
        return Ok(ToDto(applicant));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<StudentApplicantDto>> UpdateStatus(Guid id, UpdateStudentApplicantStatusRequest request)
    {
        var applicant = await _context.StudentApplicants.Include(a => a.AcademicYear).FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        if (applicant.Status is AdmissionStatus.Accepted or AdmissionStatus.Enrolled)
            return BadRequest(new { message = "Un dossier accepté ou déjà inscrit ne peut plus changer de statut ici." });

        applicant.Status = request.Status;
        applicant.DecisionNotes = request.DecisionNotes;
        applicant.DecisionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(applicant));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<StudentApplicantDto>> Accept(Guid id, AcceptApplicantRequest request)
    {
        var applicant = await _context.StudentApplicants.Include(a => a.AcademicYear).FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        if (applicant.Status is AdmissionStatus.Accepted or AdmissionStatus.Enrolled)
            return BadRequest(new { message = "Ce dossier a déjà été accepté." });

        var schoolClass = await _context.Classes.FindAsync(request.ClassId);
        if (schoolClass is null) return NotFound(new { message = "Classe introuvable." });

        var enrollmentNumber = request.EnrollmentNumber
            ?? $"MAT-{DateTime.UtcNow.Year}-{(await _context.Students.CountAsync() + 1):000}";

        var student = new Student
        {
            EnrollmentNumber = enrollmentNumber,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            DateOfBirth = applicant.DateOfBirth,
            Gender = applicant.Gender,
            EnrollmentDate = DateTime.UtcNow,
            ClassId = request.ClassId
        };

        _context.Students.Add(student);

        applicant.Status = AdmissionStatus.Enrolled;
        applicant.DecisionDate = DateTime.UtcNow;
        applicant.ConvertedStudent = student;

        await _context.SaveChangesAsync();

        return Ok(ToDto(applicant));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<StudentApplicantDto>> Reject(Guid id, [FromBody] string? notes)
    {
        var applicant = await _context.StudentApplicants.Include(a => a.AcademicYear).FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        applicant.Status = AdmissionStatus.Rejected;
        applicant.DecisionNotes = notes;
        applicant.DecisionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(applicant));
    }

    private static StudentApplicantDto ToDto(StudentApplicant a) => new(
        a.Id, a.FirstName, a.LastName, a.DateOfBirth, a.Gender.ToString(),
        a.Email, a.Phone, a.GuardianName, a.GuardianEmail, a.GuardianPhone,
        a.LevelAppliedFor, a.AcademicYearId, a.AcademicYear.Name,
        a.AppliedDate, a.Status.ToString(), a.DecisionDate, a.DecisionNotes, a.ConvertedStudentId);
}
