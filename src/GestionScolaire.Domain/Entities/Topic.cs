using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class Topic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int Order { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
}
