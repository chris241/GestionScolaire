using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EducationSettingsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public EducationSettingsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<EducationSettingsDto>> Get()
    {
        var settings = await _context.EducationSettings.FirstOrDefaultAsync();
        if (settings is null) return NotFound(new { message = "Paramètres non initialisés." });

        return Ok(new EducationSettingsDto(settings.Id, settings.SchoolName, settings.Address, settings.Currency, settings.DefaultMaxScore));
    }

    [HttpPut]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<EducationSettingsDto>> Update(UpdateEducationSettingsRequest request)
    {
        var settings = await _context.EducationSettings.FirstOrDefaultAsync();
        if (settings is null) return NotFound(new { message = "Paramètres non initialisés." });

        settings.SchoolName = request.SchoolName;
        settings.Address = request.Address;
        settings.Currency = request.Currency;
        settings.DefaultMaxScore = request.DefaultMaxScore;

        await _context.SaveChangesAsync();

        return Ok(new EducationSettingsDto(settings.Id, settings.SchoolName, settings.Address, settings.Currency, settings.DefaultMaxScore));
    }
}
