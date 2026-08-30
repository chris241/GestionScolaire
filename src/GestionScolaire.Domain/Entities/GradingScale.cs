using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class GradingScale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<GradingScaleInterval> Intervals { get; set; } = new List<GradingScaleInterval>();
}
