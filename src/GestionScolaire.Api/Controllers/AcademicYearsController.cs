using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AcademicYearsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AcademicYearsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<AcademicYearDto>>> GetAll()
    {
        var years = await _context.AcademicYears
            .OrderByDescending(y => y.StartDate)
            .Select(y => new AcademicYearDto(y.Id, y.Name, y.StartDate, y.EndDate, y.IsCurrent))
            .ToListAsync();

        return Ok(years);
    }

    [HttpGet("current")]
    public async Task<ActionResult<AcademicYearDto>> GetCurrent()
    {
        var year = await _context.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent);
        if (year is null) return NotFound(new { message = "Aucune année académique courante définie." });

        return Ok(new AcademicYearDto(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AcademicYearDto>> Create(CreateAcademicYearRequest request)
    {
        var year = new AcademicYear
        {
            Name = request.Name,
            StartDate = request.StartDate.AsUtc(),
            EndDate = request.EndDate.AsUtc(),
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.AcademicYears.Add(year);
        await _context.SaveChangesAsync();

        return Ok(new AcademicYearDto(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AcademicYearDto>> Update(Guid id, UpdateAcademicYearRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(id);
        if (year is null) return NotFound();

        year.Name = request.Name;
        year.StartDate = request.StartDate.AsUtc();
        year.EndDate = request.EndDate.AsUtc();

        await _context.SaveChangesAsync();

        return Ok(new AcademicYearDto(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent));
    }

    [HttpPost("{id:guid}/set-current")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> SetCurrent(Guid id)
    {
        var year = await _context.AcademicYears.FindAsync(id);
        if (year is null) return NotFound();

        var currentYears = await _context.AcademicYears.Where(y => y.IsCurrent).ToListAsync();
        foreach (var y in currentYears)
            y.IsCurrent = false;

        year.IsCurrent = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
