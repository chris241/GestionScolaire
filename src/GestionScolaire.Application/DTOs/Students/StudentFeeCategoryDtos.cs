namespace GestionScolaire.Application.DTOs.Students;

/// IsSubscribed vaut toujours true pour une catégorie obligatoire (facturée à tout élève actif sans
/// ligne d'abonnement) ; pour une catégorie non obligatoire, reflète l'existence d'un StudentFeeCategory.
public record StudentFeeCategoryDto(Guid FeeCategoryId, string FeeCategoryName, bool IsMandatory, bool IsSubscribed);
