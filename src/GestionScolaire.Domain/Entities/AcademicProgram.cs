using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Nommé "AcademicProgram" (et non "Program") pour éviter toute collision avec la classe
/// d'entrée générée par ASP.NET Core, référencée telle quelle dans les tests d'intégration
/// (WebApplicationFactory&lt;Program&gt;).
public class AcademicProgram : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
