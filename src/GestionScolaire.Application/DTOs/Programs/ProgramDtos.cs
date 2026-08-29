using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Programs;

public record ProgramDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int ClassCount,
    int CourseCount
);

public record CreateProgramRequest(
    [Required] string Name,
    [Required] string Code,
    string? Description
);

public record UpdateProgramRequest(
    [Required] string Name,
    [Required] string Code,
    string? Description,
    bool IsActive
);
