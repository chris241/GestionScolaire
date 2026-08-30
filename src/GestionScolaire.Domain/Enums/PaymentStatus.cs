namespace GestionScolaire.Domain.Enums;

public enum PaymentStatus
{
    EnAttente = 1,
    Paye = 2,
    EnRetard = 3,
    Annule = 4,

    /// Paiement déclaré par un Parent (méthode + référence hors app), en attente de confirmation du
    /// Directeur — distinct de EnAttente qui désigne une échéance simplement pas encore honorée.
    EnValidation = 5
}
