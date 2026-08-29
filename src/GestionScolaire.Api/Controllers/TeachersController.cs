using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Teachers;
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

    public TeachersController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> GetAll()
    {
        var teachers = await _context.Teachers
            .Include(t => t.User)
            .OrderBy(t => t.User.LastName)
            .Select(t => new TeacherDto(t.Id, $"{t.User.FirstName} {t.User.LastName}", t.Specialty))
            .ToListAsync();

        return Ok(teachers);
    }
}
