using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class Teacher : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Specialty { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }

    public ICollection<SchoolClass> HomeroomClasses { get; set; } = new List<SchoolClass>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
