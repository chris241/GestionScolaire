using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class StudentApplicant : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianPhone { get; set; }
    public string LevelAppliedFor { get; set; } = string.Empty;

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public Guid? ProgramId { get; set; }
    public AcademicProgram? Program { get; set; }

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public AdmissionStatus Status { get; set; } = AdmissionStatus.Submitted;
    public DateTime? DecisionDate { get; set; }
    public string? DecisionNotes { get; set; }

    public Guid? ConvertedStudentId { get; set; }
    public Student? ConvertedStudent { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
