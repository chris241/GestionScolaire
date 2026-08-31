using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class FeeCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// Due pour tous les élèves actifs sans abonnement explicite (ex. Scolarité) ; une catégorie non
    /// obligatoire (ex. Cantine, Transport) n'est facturée qu'aux élèves y ayant souscrit — voir
    /// StudentFeeCategory.
    public bool IsMandatory { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;
}
