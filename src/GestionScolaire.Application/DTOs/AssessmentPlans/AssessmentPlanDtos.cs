using System.ComponentModel.DataAnnotations;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Application.DTOs.AssessmentPlans;

public record AssessmentCriteriaDto(Guid Id, string Name, decimal MaxScore);

public record AssessmentPlanDto(
    Guid Id,
    string Name,
    decimal MaxScore,
    DateTime PlannedDate,
    Guid CourseId,
    string CourseName,
    Guid ClassId,
    string ClassName,
    Guid AcademicTermId,
    string AcademicTermName,
    Guid AssessmentGroupId,
    string AssessmentGroupName,
    Guid? GradingScaleId,
    string Status,
    List<AssessmentCriteriaDto> Criteria
);

public record UpdateAssessmentPlanStatusRequest([Required] AssessmentPlanStatus Status);

public record CreateAssessmentPlanRequest(
    [Required] string Name,
    [Required] decimal MaxScore,
    [Required] DateTime PlannedDate,
    [Required] Guid CourseId,
    [Required] Guid ClassId,
    [Required] Guid AcademicTermId,
    [Required] Guid AssessmentGroupId,
    Guid? GradingScaleId
);

public record CreateAssessmentCriteriaRequest([Required] string Name, [Required] decimal MaxScore);
