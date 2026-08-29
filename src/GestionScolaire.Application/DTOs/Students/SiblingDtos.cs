namespace GestionScolaire.Application.DTOs.Students;

public record SiblingDto(Guid StudentId, string StudentFullName, string EnrollmentNumber, string ClassName);
