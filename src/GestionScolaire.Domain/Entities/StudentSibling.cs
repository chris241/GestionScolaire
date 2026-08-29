using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Lien de fratrie entre deux élèves ; non orienté en usage (les deux sens sont retournés côté requête),
/// mais stocké une seule fois pour éviter les doublons.
public class StudentSibling : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid SiblingStudentId { get; set; }
    public Student SiblingStudent { get; set; } = null!;
}
