using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Fiche de contact autonome pour un tuteur/responsable légal — indépendante d'un compte de connexion,
/// contrairement à StudentParent qui relie un Student à un User pouvant se connecter au portail.
public class Guardian : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Occupation { get; set; }

    public ICollection<StudentGuardian> Students { get; set; } = new List<StudentGuardian>();

    public string FullName => $"{FirstName} {LastName}";
}
