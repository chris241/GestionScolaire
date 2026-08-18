using System.ComponentModel.DataAnnotations;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Application.DTOs.Grades;

public record CreateGradeRequest(
    [Required] Guid StudentId,
    [Required] Guid SubjectId,
    [Required] Guid ClassId,
    [Range(0, 1000)] decimal Score,
    [Range(0.01, 1000)] decimal MaxScore,
    [Range(0.1, 20)] decimal Coefficient,
    [Required] EvaluationType Type,
    [Required] string Term,
    string? Comment,
    Guid? TeacherId = null
);

public record UpdateGradeRequest(
    [Range(0, 1000)] decimal Score,
    [Range(0.01, 1000)] decimal MaxScore,
    [Range(0.1, 20)] decimal Coefficient,
    string? Comment
);

public record GradeDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid SubjectId,
    string SubjectName,
    decimal Score,
    decimal MaxScore,
    decimal Coefficient,
    string Type,
    string Term,
    DateTime EvaluatedAt,
    string? Comment
);

public record StudentAverageDto(
    Guid StudentId,
    string StudentFullName,
    string SubjectName,
    decimal Average,
    int GradeCount
);

public record StudentGeneralAverageDto(
    Guid StudentId,
    string StudentFullName,
    decimal GeneralAverage,
    List<StudentAverageDto> SubjectAverages
);
