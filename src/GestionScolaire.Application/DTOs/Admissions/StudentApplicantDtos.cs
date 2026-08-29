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
    [Required] Guid AcademicYearId
);

public record UpdateStudentApplicantStatusRequest([Required] AdmissionStatus Status, string? DecisionNotes);

public record AcceptApplicantRequest([Required] Guid ClassId, string? EnrollmentNumber);
