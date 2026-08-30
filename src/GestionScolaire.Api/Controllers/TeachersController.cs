using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Teachers;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using GestionScolaire.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TeachersController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> GetAll()
    {
        var teachers = await _context.Teachers
            .Include(t => t.User)
            .OrderBy(t => t.User.LastName)
            .Select(t => new TeacherDto(t.Id, $"{t.User.FirstName} {t.User.LastName}", t.Specialty, t.User.Email, t.HireDate))
            .ToListAsync();

        return Ok(teachers);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<TeacherDto>> Create(CreateTeacherRequest request)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict(new { message = "Un compte existe déjà avec cet email." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Teacher
        };
        _context.Users.Add(user);

        var teacher = new Teacher
        {
            User = user,
            Specialty = request.Specialty,
            HireDate = request.HireDate.AsUtc()
        };
        _context.Teachers.Add(teacher);

        // Rattache l'enseignant à l'école active du Directeur : sans ce lien TeacherSchool, l'enseignant
        // serait immédiatement invisible (filtré partout, y compris dans GetAll ci-dessus) et ne pourrait
        // jamais se connecter (aucune école accessible à la claim JWT).
        _context.TeacherSchools.Add(new TeacherSchool { Teacher = teacher, SchoolId = _currentUser.SchoolId!.Value });

        await _context.SaveChangesAsync();

        return Ok(new TeacherDto(teacher.Id, user.FullName, teacher.Specialty, user.Email, teacher.HireDate));
    }
}
