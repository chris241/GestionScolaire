using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Teachers;

public record TeacherSchoolSummaryDto(Guid Id, string Name);

public record TeacherDto(Guid Id, string FullName, string Specialty, string Email, DateTime HireDate, List<TeacherSchoolSummaryDto> Schools);

public record CreateTeacherRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Specialty,
    [Required] DateTime HireDate
);
