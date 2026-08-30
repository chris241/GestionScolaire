using System.ComponentModel.DataAnnotations;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Application.DTOs.Admissions;

public record StudentApplicantDto(
    Guid Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string? Email,
    string? Phone,
    string? GuardianName,
    string? GuardianEmail,
    string? GuardianPhone,
    string LevelAppliedFor,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? ProgramId,
    string? ProgramName,
    Guid? AdmissionCampaignId,
    string? AdmissionCampaignName,
    DateTime AppliedDate,
    string Status,
    DateTime? DecisionDate,
    string? DecisionNotes,
    Guid? ConvertedStudentId
);

public record CreateStudentApplicantRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] DateTime DateOfBirth,
    [Required] Gender Gender,
    string? Email,
    string? Phone,
    string? GuardianName,
    string? GuardianEmail,
    string? GuardianPhone,
    [Required] string LevelAppliedFor,
    [Required] Guid AcademicYearId,
    Guid? ProgramId,
    Guid? AdmissionCampaignId
);

/// Formulaire de candidature public (sans authentification) : pas d'AcademicYearId (résolu côté serveur sur
/// l'année académique courante), et coordonnées du tuteur obligatoires pour que l'établissement puisse recontacter la famille.
public record PublicApplicantRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] DateTime DateOfBirth,
    [Required] Gender Gender,
    [EmailAddress] string? Email,
    string? Phone,
    [Required] string GuardianName,
    [EmailAddress] string? GuardianEmail,
    [Required] string GuardianPhone,
    [Required] string LevelAppliedFor,
    Guid? ProgramId,
    Guid? AdmissionCampaignId
);

public record UpdateStudentApplicantStatusRequest([Required] AdmissionStatus Status, string? DecisionNotes);

public record AcceptApplicantRequest([Required] Guid ClassId, string? EnrollmentNumber);
