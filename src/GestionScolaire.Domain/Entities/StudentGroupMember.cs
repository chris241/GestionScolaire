using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentGroupMember : BaseEntity
{
    public Guid StudentGroupId { get; set; }
    public StudentGroup StudentGroup { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
}
