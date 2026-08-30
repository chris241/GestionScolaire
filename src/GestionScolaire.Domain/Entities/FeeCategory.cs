using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class FeeCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;
}
