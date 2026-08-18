using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Domain.Entities;

namespace GestionScolaire.Application.Services;

/// Calcule les moyennes pondérées par coefficient, sur note ramenée à /20.
public static class GradeAverageCalculator
{
    public static decimal CalculateSubjectAverage(IEnumerable<Grade> grades)
    {
        var list = grades.ToList();
        if (list.Count == 0) return 0;

        var weightedSum = list.Sum(g => (g.Score / g.MaxScore * 20) * g.Coefficient);
        var totalCoefficient = list.Sum(g => g.Coefficient);

        return totalCoefficient == 0 ? 0 : Math.Round(weightedSum / totalCoefficient, 2);
    }

    public static StudentGeneralAverageDto CalculateGeneralAverage(
        Guid studentId,
        string studentFullName,
        IEnumerable<Grade> allGrades)
    {
        var bySubject = allGrades
            .GroupBy(g => new { g.SubjectId, g.Subject.Name, g.Subject.Coefficient })
            .Select(group => new
            {
                group.Key.SubjectId,
                group.Key.Name,
                SubjectCoefficient = group.Key.Coefficient,
                Average = CalculateSubjectAverage(group)
            })
            .ToList();

        var subjectAverages = bySubject
            .Select(s => new StudentAverageDto(studentId, studentFullName, s.Name, s.Average, 0))
            .ToList();

        if (bySubject.Count == 0)
            return new StudentGeneralAverageDto(studentId, studentFullName, 0, subjectAverages);

        var weightedSum = bySubject.Sum(s => s.Average * s.SubjectCoefficient);
        var totalCoefficient = bySubject.Sum(s => s.SubjectCoefficient);
        var generalAverage = totalCoefficient == 0 ? 0 : Math.Round(weightedSum / totalCoefficient, 2);

        return new StudentGeneralAverageDto(studentId, studentFullName, generalAverage, subjectAverages);
    }

    public static string GetMention(decimal average) => average switch
    {
        >= 16 => "Excellent",
        >= 14 => "Très Bien",
        >= 12 => "Bien",
        >= 10 => "Assez Bien",
        _ => "Insuffisant"
    };
}
