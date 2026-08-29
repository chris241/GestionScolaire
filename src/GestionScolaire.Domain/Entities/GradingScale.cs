using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class GradingScale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<GradingScaleInterval> Intervals { get; set; } = new List<GradingScaleInterval>();
}
