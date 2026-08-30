using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Admissions;

public record AdmissionCampaignQuotaDto(
    Guid Id,
    Guid ProgramId,
    string ProgramName,
    int Quota,
    int Used,
    int Remaining
);

public record AdmissionCampaignDto(
    Guid Id,
    string Name,
    Guid AcademicYearId,
    string AcademicYearName,
    DateTime StartDate,
    DateTime EndDate,
    bool IsOpen,
    int ApplicantCount,
    List<AdmissionCampaignQuotaDto> Quotas
);

public record OpenCampaignProgramDto(Guid Id, string Name);

public record OpenAdmissionCampaignDto(Guid Id, string Name, List<OpenCampaignProgramDto> Programs);

public record CreateAdmissionCampaignRequest(
    [Required] string Name,
    [Required] Guid AcademicYearId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);

public record UpdateAdmissionCampaignRequest(
    [Required] string Name,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);

public record SetCampaignQuotaRequest(
    [Required] Guid ProgramId,
    [Range(1, 10000)] int Quota
);
