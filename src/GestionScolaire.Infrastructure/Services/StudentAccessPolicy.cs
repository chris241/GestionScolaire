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

        if (parsedRole == UserRole.Director)
            return true;

        if (parsedRole == UserRole.Teacher)
        {
            // MVP : un professeur n'accède qu'aux élèves de la ou des classes dont il est titulaire (HomeroomTeacher).
            return await _context.Students
                .Where(s => s.Id == studentId)
                .Join(_context.Classes, s => s.ClassId, c => c.Id, (s, c) => c)
                .AnyAsync(c => c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == userId);
        }

        // Parent
        return await _context.StudentParents
            .AnyAsync(sp => sp.StudentId == studentId && sp.ParentUserId == userId);
    }
}
