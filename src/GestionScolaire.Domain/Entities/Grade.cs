using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class Grade : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; } = 20;
    public decimal Coefficient { get; set; } = 1;
    public EvaluationType Type { get; set; }
    public string Term { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}
