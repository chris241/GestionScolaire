using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
