using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.AssessmentGroups;

public record AssessmentGroupDto(
    Guid Id,
    string Name,
    decimal Weightage,
    Guid AcademicTermId,
    string AcademicTermName
);

public record CreateAssessmentGroupRequest(
    [Required] string Name,
    [Required] decimal Weightage,
    [Required] Guid AcademicTermId
);
