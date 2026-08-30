using GestionScolaire.Domain.Entities;

namespace GestionScolaire.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Guid? schoolId);
    string GenerateRefreshToken();
}
