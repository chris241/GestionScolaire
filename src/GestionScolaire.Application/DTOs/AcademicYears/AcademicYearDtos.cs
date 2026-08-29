using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.AcademicYears;

public record AcademicYearDto(Guid Id, string Name, DateTime StartDate, DateTime EndDate, bool IsCurrent);

public record CreateAcademicYearRequest(
    [Required] string Name,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);

public record UpdateAcademicYearRequest(
    [Required] string Name,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);
