using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class Student : BaseEntity
{
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? PhotoUrl { get; set; }

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public Guid? StudentCategoryId { get; set; }
    public StudentCategory? StudentCategory { get; set; }

    public Guid? StudentBatchId { get; set; }
    public StudentBatch? StudentBatch { get; set; }

    /// Compte de connexion de l'élève lui-même (portail élève) ; nullable, la plupart des élèves n'en ont pas.
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<StudentParent> Parents { get; set; } = new List<StudentParent>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<StudentGroupMember> GroupMemberships { get; set; } = new List<StudentGroupMember>();
    public ICollection<StudentLog> Logs { get; set; } = new List<StudentLog>();

    public string FullName => $"{FirstName} {LastName}";
}
