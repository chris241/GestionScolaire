using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class FeeStructureItem : BaseEntity
{
    public Guid FeeStructureId { get; set; }
    public FeeStructure FeeStructure { get; set; } = null!;

    public Guid FeeCategoryId { get; set; }
    public FeeCategory FeeCategory { get; set; } = null!;

    public decimal Amount { get; set; }
}
