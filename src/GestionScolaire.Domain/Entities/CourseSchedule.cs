using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class CourseSchedule : BaseEntity
{
    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    /// L'enseignant fait office d'intervenant — pas d'entité Instructor séparée (simplification MVP).
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid? ClassId { get; set; }
    public SchoolClass? Class { get; set; }

    public Guid AcademicTermId { get; set; }
    public AcademicTerm AcademicTerm { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
