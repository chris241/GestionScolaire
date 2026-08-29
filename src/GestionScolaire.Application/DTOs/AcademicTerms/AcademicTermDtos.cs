using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.AcademicTerms;

public record AcademicTermDto(
    Guid Id,
    string Name,
    int Order,
    DateTime StartDate,
    DateTime EndDate,
    Guid AcademicYearId,
    string AcademicYearName
);

public record CreateAcademicTermRequest(
    [Required] string Name,
    [Required] int Order,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] Guid AcademicYearId
);

public record UpdateAcademicTermRequest(
    [Required] string Name,
    [Required] int Order,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);
