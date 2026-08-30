using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public int? MaxSize { get; set; }

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid? ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public ICollection<StudentGroupMember> Members { get; set; } = new List<StudentGroupMember>();
}
