using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.GradingScales;

public record GradingScaleIntervalDto(Guid Id, string Grade, decimal MinScore, decimal MaxScore);

public record GradingScaleDto(Guid Id, string Name, bool IsDefault, List<GradingScaleIntervalDto> Intervals);

public record CreateGradingScaleRequest([Required] string Name, bool IsDefault);

public record CreateGradingScaleIntervalRequest(
    [Required] string Grade,
    [Required] decimal MinScore,
    [Required] decimal MaxScore
);
