using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Enrobe une Subject existante avec une appartenance à un programme et une planification —
/// ne remplace pas Subject, qui reste la référence utilisée par Grade.
public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public Guid ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<CourseSchedule> Schedules { get; set; } = new List<CourseSchedule>();
}
