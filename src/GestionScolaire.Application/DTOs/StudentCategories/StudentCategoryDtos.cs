using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.StudentCategories;

public record StudentCategoryDto(Guid Id, string Name, string? Description);

public record CreateStudentCategoryRequest([Required] string Name, string? Description);

public record UpdateStudentCategoryRequest([Required] string Name, string? Description);
