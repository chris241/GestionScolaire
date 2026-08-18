using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.EnAttente;
    public string? Method { get; set; }
    public string? InvoiceNumber { get; set; }
}
