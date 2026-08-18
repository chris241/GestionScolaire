namespace GestionScolaire.Application.Common.Interfaces;

/// Détermine si l'utilisateur courant a le droit de consulter les données d'un élève donné.
/// Director et Teacher ont un accès total ; un Parent n'accède qu'aux élèves qui lui sont rattachés (StudentParent).
public interface IStudentAccessPolicy
{
    Task<bool> CanAccessStudentAsync(Guid userId, string role, Guid studentId);
}
