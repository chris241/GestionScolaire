using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.AcademicTerms;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AcademicTermsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AcademicTermsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<AcademicTermDto>>> GetAll([FromQuery] Guid? academicYearId)
    {
        var query = _context.AcademicTerms.Include(t => t.AcademicYear).AsQueryable();

        if (academicYearId.HasValue)
            query = query.Where(t => t.AcademicYearId == academicYearId.Value);

        var terms = await query
            .OrderBy(t => t.Order)
            .Select(t => new AcademicTermDto(t.Id, t.Name, t.Order, t.StartDate, t.EndDate, t.AcademicYearId, t.AcademicYear.Name))
            .ToListAsync();

        return Ok(terms);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AcademicTermDto>> Create(CreateAcademicTermRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        var term = new AcademicTerm
        {
            Name = request.Name,
            Order = request.Order,
            StartDate = request.StartDate.AsUtc(),
            EndDate = request.EndDate.AsUtc(),
            AcademicYearId = request.AcademicYearId,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.AcademicTerms.Add(term);
        await _context.SaveChangesAsync();

        return Ok(new AcademicTermDto(term.Id, term.Name, term.Order, term.StartDate, term.EndDate, term.AcademicYearId, year.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AcademicTermDto>> Update(Guid id, UpdateAcademicTermRequest request)
    {
        var term = await _context.AcademicTerms.Include(t => t.AcademicYear).FirstOrDefaultAsync(t => t.Id == id);
        if (term is null) return NotFound();

        term.Name = request.Name;
        term.Order = request.Order;
        term.StartDate = request.StartDate.AsUtc();
        term.EndDate = request.EndDate.AsUtc();

        await _context.SaveChangesAsync();

        return Ok(new AcademicTermDto(term.Id, term.Name, term.Order, term.StartDate, term.EndDate, term.AcademicYearId, term.AcademicYear.Name));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var term = await _context.AcademicTerms.FindAsync(id);
        if (term is null) return NotFound();

        _context.AcademicTerms.Remove(term);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
