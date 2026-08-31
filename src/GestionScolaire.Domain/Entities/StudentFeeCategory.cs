using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Abonnement d'un élève à une catégorie de frais non obligatoire (ex. Cantine, Transport) — seules les
/// catégories non obligatoires ont besoin d'un abonnement explicite, une catégorie obligatoire
/// (FeeCategory.IsMandatory) s'applique à tout élève actif sans qu'une ligne existe ici.
public class StudentFeeCategory : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid FeeCategoryId { get; set; }
    public FeeCategory FeeCategory { get; set; } = null!;
}
