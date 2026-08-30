using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Jonction many-to-many : un enseignant peut couvrir plusieurs écoles du même directeur.
public class TeacherSchool : BaseEntity
{
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;
}
