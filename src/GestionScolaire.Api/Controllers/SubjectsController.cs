using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SubjectsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAll()
    {
        var subjects = await _context.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto(s.Id, s.Name, s.Coefficient))
            .ToListAsync();

        return Ok(subjects);
    }
}
