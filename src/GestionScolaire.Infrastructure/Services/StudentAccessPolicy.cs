using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Infrastructure.Services;

public class StudentAccessPolicy : IStudentAccessPolicy
{
    private readonly IApplicationDbContext _context;

    public StudentAccessPolicy(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessStudentAsync(Guid userId, string role, Guid studentId)
    {
        if (!Enum.TryParse<UserRole>(role, out var parsedRole))
            return false;

        if (parsedRole is UserRole.Director or UserRole.Teacher)
            return true;

        return await _context.StudentParents
            .AnyAsync(sp => sp.StudentId == studentId && sp.ParentUserId == userId);
    }
}
