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
    string? Method,
    string? DecisionNotes,
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

/// Un Parent déclare avoir réglé un montant hors app (espèces remis à l'école, Mobile Money, virement) ;
/// le paiement reste "en attente de validation" jusqu'à ce que le Directeur le confirme.
public record DeclarePaymentRequest(
    [Required] Guid StudentId,
    [Required] string Description,
    [Required] decimal Amount,
    [Required] string AcademicYear,
    [Required] string Term,
    [Required] string Method,
    Guid? InvoiceId
);

public record RejectPaymentRequest(string? DecisionNotes);
