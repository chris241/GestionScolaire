using System.Security.Claims;
using GestionScolaire.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestionScolaire.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.Claims
        .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
}
