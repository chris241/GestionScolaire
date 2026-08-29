using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Settings;

public record EducationSettingsDto(Guid Id, string SchoolName, string? Address, string Currency, decimal DefaultMaxScore);

public record UpdateEducationSettingsRequest(
    [Required] string SchoolName,
    string? Address,
    [Required] string Currency,
    [Required, Range(0.01, 1000)] decimal DefaultMaxScore
);
