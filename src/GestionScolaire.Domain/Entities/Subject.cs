using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Coefficient { get; set; } = 1;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
