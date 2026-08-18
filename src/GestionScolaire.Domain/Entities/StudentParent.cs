using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Table de jointure many-to-many : un parent peut suivre plusieurs enfants, un enfant peut avoir plusieurs tuteurs.
public class StudentParent : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid ParentUserId { get; set; }
    public User ParentUser { get; set; } = null!;

    public string Relationship { get; set; } = "Parent";
}
