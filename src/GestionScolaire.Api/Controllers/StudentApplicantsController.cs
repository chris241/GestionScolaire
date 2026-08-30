using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        var query = BaseQuery();

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
        var applicant = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        return Ok(ToDto(applicant));
    }

    [HttpPost]
    public async Task<ActionResult<StudentApplicantDto>> Create(CreateStudentApplicantRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        var (campaignError, program) = await ValidateCampaignAndProgramAsync(request.AdmissionCampaignId, request.ProgramId);
        if (campaignError is not null) return BadRequest(new { message = campaignError });

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
            ProgramId = request.ProgramId,
            AdmissionCampaignId = request.AdmissionCampaignId,
            Status = AdmissionStatus.Submitted
        };

        _context.StudentApplicants.Add(applicant);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(a => a.Id == applicant.Id);
        return Ok(ToDto(full));
    }

    /// Formulaire public de candidature : accessible sans compte, pour que les familles postulent directement.
    /// Limité en débit par IP (aucun CAPTCHA disponible) ; le statut est toujours « Soumis » et l'année académique
    /// est résolue côté serveur pour ne pas exposer d'identifiants internes à un visiteur anonyme.
    [HttpPost("public")]
    [AllowAnonymous]
    [EnableRateLimiting("public-form")]
    public async Task<ActionResult<StudentApplicantDto>> CreatePublic(PublicApplicantRequest request)
    {
        // Visiteur anonyme, donc sans contexte école : StudentApplicant n'est pas encore scopé par école
        // (prévu en phase 2 du plan multi-établissements), on ignore donc le filtre pour l'instant.
        var year = await _context.AcademicYears.IgnoreQueryFilters().FirstOrDefaultAsync(y => y.IsCurrent);
        if (year is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Les candidatures ne sont pas ouvertes pour le moment." });

        var dateOfBirth = request.DateOfBirth.AsUtc();
        if (dateOfBirth > DateTime.UtcNow)
            return BadRequest(new { message = "Date de naissance invalide." });

        var (campaignError, program) = await ValidateCampaignAndProgramAsync(request.AdmissionCampaignId, request.ProgramId);
        if (campaignError is not null) return BadRequest(new { message = campaignError });

        var applicant = new StudentApplicant
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = dateOfBirth,
            Gender = request.Gender,
            Email = request.Email,
            Phone = request.Phone,
            GuardianName = request.GuardianName,
            GuardianEmail = request.GuardianEmail,
            GuardianPhone = request.GuardianPhone,
            LevelAppliedFor = request.LevelAppliedFor,
            AcademicYearId = year.Id,
            ProgramId = request.ProgramId,
            AdmissionCampaignId = request.AdmissionCampaignId,
            Status = AdmissionStatus.Submitted
        };

        _context.StudentApplicants.Add(applicant);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(a => a.Id == applicant.Id);
        return Ok(ToDto(full));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<StudentApplicantDto>> UpdateStatus(Guid id, UpdateStudentApplicantStatusRequest request)
    {
        var applicant = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
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
        var applicant = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        if (applicant.Status is AdmissionStatus.Accepted or AdmissionStatus.Enrolled)
            return BadRequest(new { message = "Ce dossier a déjà été accepté." });

        var schoolClass = await _context.Classes.FindAsync(request.ClassId);
        if (schoolClass is null) return NotFound(new { message = "Classe introuvable." });

        if (applicant.AdmissionCampaignId.HasValue && applicant.ProgramId.HasValue)
        {
            var quota = await _context.AdmissionCampaignQuotas.FirstOrDefaultAsync(q =>
                q.AdmissionCampaignId == applicant.AdmissionCampaignId.Value && q.ProgramId == applicant.ProgramId.Value);

            if (quota is not null)
            {
                var acceptedCount = await _context.StudentApplicants.CountAsync(a =>
                    a.AdmissionCampaignId == applicant.AdmissionCampaignId.Value &&
                    a.ProgramId == applicant.ProgramId.Value &&
                    a.Id != applicant.Id &&
                    (a.Status == AdmissionStatus.Accepted || a.Status == AdmissionStatus.Enrolled));

                if (acceptedCount >= quota.Quota)
                    return Conflict(new { message = "Le quota de ce programme pour cette campagne est atteint." });
            }
        }

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
        var applicant = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
        if (applicant is null) return NotFound();

        applicant.Status = AdmissionStatus.Rejected;
        applicant.DecisionNotes = notes;
        applicant.DecisionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToDto(applicant));
    }

    private async Task<(string? Error, AcademicProgram? Program)> ValidateCampaignAndProgramAsync(Guid? campaignId, Guid? programId)
    {
        if (programId.HasValue)
        {
            // Peut être appelé depuis le formulaire public (anonyme, sans contexte école) : AcademicProgram
            // n'est pas non plus scopé côté StudentApplicant pour l'instant (voir phase 2).
            var program = await _context.AcademicPrograms.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == programId.Value);
            if (program is null) return ("Programme introuvable.", null);
        }

        if (campaignId.HasValue)
        {
            var campaign = await _context.AdmissionCampaigns.FindAsync(campaignId.Value);
            if (campaign is null) return ("Campagne d'admission introuvable.", null);

            var now = DateTime.UtcNow;
            if (now < campaign.StartDate || now > campaign.EndDate)
                return ("Cette campagne d'admission n'est pas ouverte actuellement.", null);
        }

        return (null, null);
    }

    private IQueryable<StudentApplicant> BaseQuery() => _context.StudentApplicants.IgnoreQueryFilters()
        .Include(a => a.AcademicYear)
        .Include(a => a.Program)
        .Include(a => a.AdmissionCampaign);

    private static StudentApplicantDto ToDto(StudentApplicant a) => new(
        a.Id, a.FirstName, a.LastName, a.DateOfBirth, a.Gender.ToString(),
        a.Email, a.Phone, a.GuardianName, a.GuardianEmail, a.GuardianPhone,
        a.LevelAppliedFor, a.AcademicYearId, a.AcademicYear.Name,
        a.ProgramId, a.Program?.Name,
        a.AdmissionCampaignId, a.AdmissionCampaign?.Name,
        a.AppliedDate, a.Status.ToString(), a.DecisionDate, a.DecisionNotes, a.ConvertedStudentId);
}
