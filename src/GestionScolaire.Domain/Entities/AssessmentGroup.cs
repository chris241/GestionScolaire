using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Catégorie d'évaluation (ex: "Devoirs", "Compositions") pondérée dans la moyenne finale d'un trimestre.
public class AssessmentGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Weightage { get; set; } = 100;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid AcademicTermId { get; set; }
    public AcademicTerm AcademicTerm { get; set; } = null!;

    public ICollection<AssessmentPlan> Plans { get; set; } = new List<AssessmentPlan>();
}
