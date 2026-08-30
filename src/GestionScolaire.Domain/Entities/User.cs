using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public Guid? LastActiveSchoolId { get; set; }

    public Teacher? Teacher { get; set; }
    public ICollection<StudentParent> Children { get; set; } = new List<StudentParent>();

    public string FullName => $"{FirstName} {LastName}";
}
