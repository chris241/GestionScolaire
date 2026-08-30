using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Schools;

public record SchoolDto(
    Guid Id,
    string Name,
    string? Address,
    string Currency,
    decimal DefaultMaxScore,
    Guid DirectorId,
    bool IsActive
);

public record CreateSchoolRequest(
    [Required] string Name,
    string? Address,
    [Required] string Currency,
    [Required, Range(0.01, 1000)] decimal DefaultMaxScore
);

public record UpdateSchoolRequest(
    [Required] string Name,
    string? Address,
    [Required] string Currency,
    [Required, Range(0.01, 1000)] decimal DefaultMaxScore
);

/// Représentation minimale exposée sans authentification (formulaire public de candidature) :
/// aucune donnée sensible (adresse, devise, DirectorId...).
public record PublicSchoolDto(Guid Id, string Name);
