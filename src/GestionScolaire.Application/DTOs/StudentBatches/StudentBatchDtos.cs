using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.StudentBatches;

public record StudentBatchDto(
    Guid Id,
    string Name,
    DateTime StartDate,
    DateTime? EndDate,
    string? Description,
    Guid AcademicYearId,
    string AcademicYearName
);

public record CreateStudentBatchRequest(
    [Required] string Name,
    [Required] DateTime StartDate,
    DateTime? EndDate,
    string? Description,
    [Required] Guid AcademicYearId
);

public record UpdateStudentBatchRequest(
    [Required] string Name,
    [Required] DateTime StartDate,
    DateTime? EndDate,
    string? Description
);
