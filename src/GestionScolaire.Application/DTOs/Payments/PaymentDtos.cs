using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Payments;

public record PaymentDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    string Description,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaidAt,
    string Status,
    Guid? InvoiceId = null
);

/// Le Directeur enregistre un paiement déjà reçu (espèces, Mobile Money...) ; aucune intégration
/// de paiement en ligne — hors périmètre.
public record CreatePaymentRequest(
    [Required] Guid StudentId,
    [Required] string Description,
    [Required] decimal Amount,
    [Required] string AcademicYear,
    [Required] string Term,
    [Required] string Method,
    Guid? InvoiceId
);
