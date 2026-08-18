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

    public ICollection<StudentParent> Parents { get; set; } = new List<StudentParent>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public string FullName => $"{FirstName} {LastName}";
}
