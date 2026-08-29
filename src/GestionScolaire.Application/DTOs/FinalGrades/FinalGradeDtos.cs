using GestionScolaire.Application.DTOs.Grades;

namespace GestionScolaire.Application.DTOs.FinalGrades;

public record FinalGradeDto(
    Guid StudentId,
    string StudentFullName,
    decimal GeneralAverage,
    string Mention,
    string? LetterGrade,
    int ClassRank,
    int ClassSize,
    List<StudentAverageDto> SubjectAverages
);
