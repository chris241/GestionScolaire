using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Coefficient { get; set; } = 1;

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
