using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class AcademicYear : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<AcademicTerm> Terms { get; set; } = new List<AcademicTerm>();
    public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
}
