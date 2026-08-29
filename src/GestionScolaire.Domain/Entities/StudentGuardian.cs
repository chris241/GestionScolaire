using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentGuardian : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid GuardianId { get; set; }
    public Guardian Guardian { get; set; } = null!;

    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimaryContact { get; set; }
}
