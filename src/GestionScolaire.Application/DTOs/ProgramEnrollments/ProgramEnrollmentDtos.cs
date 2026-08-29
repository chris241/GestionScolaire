using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.ProgramEnrollments;

public record ProgramEnrollmentDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid ProgramId,
    string ProgramName,
    Guid AcademicYearId,
    string AcademicYearName,
    DateTime EnrollmentDate,
    string Status
);

public record CreateProgramEnrollmentRequest(
    [Required] Guid StudentId,
    [Required] Guid ProgramId,
    [Required] Guid AcademicYearId
);

public record BulkEnrollRequest(
    [Required] List<Guid> StudentIds,
    [Required] Guid ProgramId,
    [Required] Guid AcademicYearId
);
