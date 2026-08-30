using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentBatch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
