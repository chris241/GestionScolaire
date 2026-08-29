using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.GradingScales;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradingScalesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public GradingScalesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GradingScaleDto>>> GetAll()
    {
        var scales = await _context.GradingScales.Include(s => s.Intervals).OrderBy(s => s.Name).ToListAsync();
        return Ok(scales.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<GradingScaleDto>> Create(CreateGradingScaleRequest request)
    {
        var scale = new GradingScale { Name = request.Name, IsDefault = request.IsDefault };

        if (request.IsDefault)
        {
            var currentDefaults = await _context.GradingScales.Where(s => s.IsDefault).ToListAsync();
            foreach (var d in currentDefaults) d.IsDefault = false;
        }

        _context.GradingScales.Add(scale);
        await _context.SaveChangesAsync();

        return Ok(ToDto(scale));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var scale = await _context.GradingScales.FindAsync(id);
        if (scale is null) return NotFound();

        _context.GradingScales.Remove(scale);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/intervals")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<GradingScaleIntervalDto>> AddInterval(Guid id, CreateGradingScaleIntervalRequest request)
    {
        var scale = await _context.GradingScales.FindAsync(id);
        if (scale is null) return NotFound();

        if (request.MaxScore < request.MinScore)
            return BadRequest(new { message = "Le score maximum doit être supérieur ou égal au score minimum." });

        var interval = new GradingScaleInterval
        {
            GradingScaleId = id,
            Grade = request.Grade,
            MinScore = request.MinScore,
            MaxScore = request.MaxScore
        };

        _context.GradingScaleIntervals.Add(interval);
        await _context.SaveChangesAsync();

        return Ok(new GradingScaleIntervalDto(interval.Id, interval.Grade, interval.MinScore, interval.MaxScore));
    }

    [HttpDelete("intervals/{intervalId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> DeleteInterval(Guid intervalId)
    {
        var interval = await _context.GradingScaleIntervals.FindAsync(intervalId);
        if (interval is null) return NotFound();

        _context.GradingScaleIntervals.Remove(interval);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static GradingScaleDto ToDto(GradingScale s) => new(
        s.Id, s.Name, s.IsDefault,
        s.Intervals.OrderByDescending(i => i.MinScore).Select(i => new GradingScaleIntervalDto(i.Id, i.Grade, i.MinScore, i.MaxScore)).ToList());
}
