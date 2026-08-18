namespace GestionScolaire.Application.DTOs.Students;

public record StudentDto(
    Guid Id,
    string EnrollmentNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    Guid ClassId,
    string ClassName,
    bool IsActive
);
