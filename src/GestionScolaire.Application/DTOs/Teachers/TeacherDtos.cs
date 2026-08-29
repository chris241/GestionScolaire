using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Teachers;

public record TeacherDto(Guid Id, string FullName, string Specialty, string Email, DateTime HireDate);

public record CreateTeacherRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Specialty,
    [Required] DateTime HireDate
);
