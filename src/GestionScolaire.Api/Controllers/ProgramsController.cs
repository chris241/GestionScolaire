using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgramsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProgramsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProgramDto>>> GetAll()
    {
        var programs = await _context.AcademicPrograms
            .Include(p => p.Classes)
            .Include(p => p.Courses)
            .OrderBy(p => p.Name)
            .Select(p => new ProgramDto(p.Id, p.Name, p.Code, p.Description, p.IsActive, p.Classes.Count, p.Courses.Count))
            .ToListAsync();

        return Ok(programs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProgramDto>> GetById(Guid id)
    {
        var program = await _context.AcademicPrograms
            .Include(p => p.Classes)
            .Include(p => p.Courses)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program is null) return NotFound();

        return Ok(new ProgramDto(program.Id, program.Name, program.Code, program.Description, program.IsActive, program.Classes.Count, program.Courses.Count));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<ProgramDto>> Create(CreateProgramRequest request)
    {
        var program = new AcademicProgram
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.AcademicPrograms.Add(program);
        await _context.SaveChangesAsync();

        return Ok(new ProgramDto(program.Id, program.Name, program.Code, program.Description, program.IsActive, 0, 0));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<ProgramDto>> Update(Guid id, UpdateProgramRequest request)
    {
        var program = await _context.AcademicPrograms
            .Include(p => p.Classes)
            .Include(p => p.Courses)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program is null) return NotFound();

        program.Name = request.Name;
        program.Code = request.Code;
        program.Description = request.Description;
        program.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new ProgramDto(program.Id, program.Name, program.Code, program.Description, program.IsActive, program.Classes.Count, program.Courses.Count));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var program = await _context.AcademicPrograms.FindAsync(id);
        if (program is null) return NotFound();

        _context.AcademicPrograms.Remove(program);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
