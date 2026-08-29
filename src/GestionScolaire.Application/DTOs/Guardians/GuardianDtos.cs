using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Guardians;

public record GuardianDto(Guid Id, string FirstName, string LastName, string FullName, string Phone, string? Email, string? Occupation);

public record CreateGuardianRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string Phone,
    [EmailAddress] string? Email,
    string? Occupation
);

public record StudentGuardianDto(
    Guid Id,
    Guid GuardianId,
    string GuardianFullName,
    string Phone,
    string? Email,
    string? Occupation,
    string Relationship,
    bool IsPrimaryContact
);

public record LinkGuardianRequest([Required] string Relationship, bool IsPrimaryContact);
