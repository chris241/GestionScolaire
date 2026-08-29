using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Modèle de frais applicable à une année académique, optionnellement limité à un programme
/// (null = s'applique à tous les programmes).
public class FeeStructure : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public Guid? ProgramId { get; set; }
    public AcademicProgram? Program { get; set; }

    public ICollection<FeeStructureItem> Items { get; set; } = new List<FeeStructureItem>();
    public ICollection<FeeSchedule> Schedules { get; set; } = new List<FeeSchedule>();
}
