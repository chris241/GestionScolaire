using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Un établissement, propriété d'un seul Directeur (User.Role == Director). Un directeur peut posséder
/// plusieurs écoles ; chaque école a ses propres classes, salles, etc. (isolées via un filtre global EF Core
/// sur SchoolId, voir AppDbContext.OnModelCreating).
public class School : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Currency { get; set; } = "MGA";
    public decimal DefaultMaxScore { get; set; } = 20;
    public bool IsActive { get; set; } = true;

    public Guid DirectorId { get; set; }
    public User Director { get; set; } = null!;

    public ICollection<TeacherSchool> Teachers { get; set; } = new List<TeacherSchool>();
}
