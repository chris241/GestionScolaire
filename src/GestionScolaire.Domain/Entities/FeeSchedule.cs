using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Échéance d'un modèle de frais pour un trimestre donné ; sert de base à la génération des factures.
public class FeeSchedule : BaseEntity
{
    public Guid FeeStructureId { get; set; }
    public FeeStructure FeeStructure { get; set; } = null!;

    public Guid AcademicTermId { get; set; }
    public AcademicTerm AcademicTerm { get; set; } = null!;

    public DateTime DueDate { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
