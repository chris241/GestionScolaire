using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class GradingScaleInterval : BaseEntity
{
    public Guid GradingScaleId { get; set; }
    public GradingScale GradingScale { get; set; } = null!;

    public string Grade { get; set; } = string.Empty;
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
}
