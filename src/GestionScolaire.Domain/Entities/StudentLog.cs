using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class StudentLog : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime LogDate { get; set; } = DateTime.UtcNow;
    public string LogType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RecordedByUserId { get; set; }
}
