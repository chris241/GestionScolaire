using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StudentsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentDto>>> GetAll([FromQuery] Guid? classId)
    {
        var query = _context.Students.Include(s => s.Class).AsQueryable();

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        if (_currentUser.Role == nameof(UserRole.Parent))
        {
            var childIds = _context.StudentParents
                .Where(sp => sp.ParentUserId == _currentUser.UserId)
                .Select(sp => sp.StudentId);

            query = query.Where(s => childIds.Contains(s.Id));
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .Select(s => new StudentDto(
                s.Id, s.EnrollmentNumber, s.FirstName, s.LastName,
                s.DateOfBirth, s.Gender.ToString(), s.ClassId, s.Class.Name, s.IsActive))
            .ToListAsync();

        return Ok(students);
    }
}
