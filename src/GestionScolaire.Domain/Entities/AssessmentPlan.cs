using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class AssessmentPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; } = 20;
    public DateTime PlannedDate { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public Guid AcademicTermId { get; set; }
    public AcademicTerm AcademicTerm { get; set; } = null!;

    public Guid AssessmentGroupId { get; set; }
    public AssessmentGroup AssessmentGroup { get; set; } = null!;

    public Guid? GradingScaleId { get; set; }
    public GradingScale? GradingScale { get; set; }

    public ICollection<AssessmentCriteria> Criteria { get; set; } = new List<AssessmentCriteria>();
}
