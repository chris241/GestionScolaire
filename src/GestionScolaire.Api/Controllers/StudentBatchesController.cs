using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.StudentBatches;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentBatchesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StudentBatchesController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentBatchDto>>> GetAll()
    {
        var batches = await _context.StudentBatches
            .Include(b => b.AcademicYear)
            .OrderByDescending(b => b.StartDate)
            .Select(b => new StudentBatchDto(b.Id, b.Name, b.StartDate, b.EndDate, b.Description, b.AcademicYearId, b.AcademicYear.Name))
            .ToListAsync();

        return Ok(batches);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentBatchDto>> Create(CreateStudentBatchRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        var batch = new StudentBatch
        {
            Name = request.Name,
            StartDate = request.StartDate.AsUtc(),
            EndDate = request.EndDate?.AsUtc(),
            Description = request.Description,
            AcademicYearId = request.AcademicYearId,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.StudentBatches.Add(batch);
        await _context.SaveChangesAsync();

        return Ok(new StudentBatchDto(batch.Id, batch.Name, batch.StartDate, batch.EndDate, batch.Description, batch.AcademicYearId, year.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentBatchDto>> Update(Guid id, UpdateStudentBatchRequest request)
    {
        var batch = await _context.StudentBatches.Include(b => b.AcademicYear).FirstOrDefaultAsync(b => b.Id == id);
        if (batch is null) return NotFound();

        batch.Name = request.Name;
        batch.StartDate = request.StartDate.AsUtc();
        batch.EndDate = request.EndDate?.AsUtc();
        batch.Description = request.Description;
        await _context.SaveChangesAsync();

        return Ok(new StudentBatchDto(batch.Id, batch.Name, batch.StartDate, batch.EndDate, batch.Description, batch.AcademicYearId, batch.AcademicYear.Name));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var batch = await _context.StudentBatches.FindAsync(id);
        if (batch is null) return NotFound();

        _context.StudentBatches.Remove(batch);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
