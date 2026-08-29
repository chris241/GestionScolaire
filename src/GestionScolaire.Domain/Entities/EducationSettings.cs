using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Ligne unique de paramètres globaux de l'établissement (aucune contrainte DB dédiée : une seule ligne est semée).
public class EducationSettings : BaseEntity
{
    public string SchoolName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Currency { get; set; } = "MGA";
    public decimal DefaultMaxScore { get; set; } = 20;
}
