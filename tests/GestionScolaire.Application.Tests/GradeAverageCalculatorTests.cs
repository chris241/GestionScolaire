using GestionScolaire.Application.Services;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Application.Tests;

public class GradeAverageCalculatorTests
{
    private static Grade MakeGrade(Subject subject, decimal score, decimal maxScore = 20, decimal? coefficient = null) => new()
    {
        Subject = subject,
        SubjectId = subject.Id,
        Score = score,
        MaxScore = maxScore,
        Coefficient = coefficient ?? subject.Coefficient,
        Type = EvaluationType.Devoir,
        Term = "Trimestre 1"
    };

    [Fact]
    public void CalculateSubjectAverage_ReturnsZero_WhenNoGrades()
    {
        var result = GradeAverageCalculator.CalculateSubjectAverage(Enumerable.Empty<Grade>());

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateSubjectAverage_WeightsByCoefficient()
    {
        var math = new Subject { Name = "Mathématiques", Coefficient = 1 };
        var grades = new[]
        {
            MakeGrade(math, score: 10, coefficient: 1),
            MakeGrade(math, score: 20, coefficient: 3),
        };

        // (10*1 + 20*3) / (1+3) = 70/4 = 17.5
        var result = GradeAverageCalculator.CalculateSubjectAverage(grades);

        Assert.Equal(17.5m, result);
    }

    [Fact]
    public void CalculateSubjectAverage_NormalizesScoreToTwenty_WhenMaxScoreDiffers()
    {
        var math = new Subject { Name = "Mathématiques", Coefficient = 1 };
        var grades = new[] { MakeGrade(math, score: 8, maxScore: 10, coefficient: 1) };

        // 8/10 * 20 = 16
        var result = GradeAverageCalculator.CalculateSubjectAverage(grades);

        Assert.Equal(16m, result);
    }

    [Fact]
    public void CalculateGeneralAverage_ReturnsZero_WhenStudentHasNoGrades()
    {
        var result = GradeAverageCalculator.CalculateGeneralAverage(Guid.NewGuid(), "Élève Test", Enumerable.Empty<Grade>());

        Assert.Equal(0, result.GeneralAverage);
        Assert.Empty(result.SubjectAverages);
    }

    [Fact]
    public void CalculateGeneralAverage_WeightsSubjectsByTheirCoefficient()
    {
        var math = new Subject { Name = "Mathématiques", Coefficient = 4 };
        var french = new Subject { Name = "Français", Coefficient = 2 };
        var studentId = Guid.NewGuid();

        var grades = new[]
        {
            MakeGrade(math, score: 15),   // moyenne matière = 15, coeff 4
            MakeGrade(french, score: 9),  // moyenne matière = 9, coeff 2
        };

        // (15*4 + 9*2) / (4+2) = 78/6 = 13
        var result = GradeAverageCalculator.CalculateGeneralAverage(studentId, "Élève Test", grades);

        Assert.Equal(13m, result.GeneralAverage);
        Assert.Equal(2, result.SubjectAverages.Count);
    }

    [Theory]
    [InlineData(17, "Excellent")]
    [InlineData(16, "Excellent")]
    [InlineData(15, "Très Bien")]
    [InlineData(14, "Très Bien")]
    [InlineData(13, "Bien")]
    [InlineData(12, "Bien")]
    [InlineData(11, "Assez Bien")]
    [InlineData(10, "Assez Bien")]
    [InlineData(9.9, "Insuffisant")]
    [InlineData(0, "Insuffisant")]
    public void GetMention_ReturnsExpectedLabel(double average, string expectedMention)
    {
        Assert.Equal(expectedMention, GradeAverageCalculator.GetMention((decimal)average));
    }
}
