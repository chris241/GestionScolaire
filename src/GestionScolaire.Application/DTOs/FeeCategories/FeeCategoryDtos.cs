using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.FeeCategories;

public record FeeCategoryDto(Guid Id, string Name, string? Description);

public record CreateFeeCategoryRequest([Required] string Name, string? Description);
