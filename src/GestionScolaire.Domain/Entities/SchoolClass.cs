using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class SchoolClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public Guid? HomeroomTeacherId { get; set; }
    public Teacher? HomeroomTeacher { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
