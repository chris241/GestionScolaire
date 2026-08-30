using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

/// Inscription individuelle d'un élève à un cours précis (typiquement une option) — distincte de
/// ProgramEnrollment, qui rattache l'élève au programme dans son ensemble. Un élève ne peut s'inscrire
/// à un cours que si son ProgramEnrollment couvre déjà le programme auquel ce cours appartient.
public class CourseEnrollment : BaseEntity
{
    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}
