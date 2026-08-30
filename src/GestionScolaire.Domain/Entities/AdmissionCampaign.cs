using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

/// Fenêtre de candidature avec dates d'ouverture/fermeture et quotas par programme —
/// distincte du statut individuel porté par StudentApplicant.
public class AdmissionCampaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;

    public ICollection<AdmissionCampaignQuota> Quotas { get; set; } = new List<AdmissionCampaignQuota>();
    public ICollection<StudentApplicant> Applicants { get; set; } = new List<StudentApplicant>();
}

public class AdmissionCampaignQuota : BaseEntity
{
    public Guid AdmissionCampaignId { get; set; }
    public AdmissionCampaign AdmissionCampaign { get; set; } = null!;

    public Guid ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;

    public int Quota { get; set; }
}
