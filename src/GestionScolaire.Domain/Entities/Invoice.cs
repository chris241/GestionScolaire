using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid FeeScheduleId { get; set; }
    public FeeSchedule FeeSchedule { get; set; } = null!;

    /// Catégorie de frais précise couverte par cette facture (une facture = un élève, une échéance,
    /// une catégorie) — permet de savoir, par exemple, que la Cantine d'octobre est payée mais pas le
    /// Transport d'octobre pour le même élève.
    public Guid FeeStructureItemId { get; set; }
    public FeeStructureItem FeeStructureItem { get; set; } = null!;

    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.EnAttente;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
