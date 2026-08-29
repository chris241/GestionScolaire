using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class AssessmentCriteria : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; } = 20;

    public Guid AssessmentPlanId { get; set; }
    public AssessmentPlan AssessmentPlan { get; set; } = null!;
}
