namespace GestionScolaire.Application.DTOs.Payments;

public record PaymentDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    string Description,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaidAt,
    string Status
);
