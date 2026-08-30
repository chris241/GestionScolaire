using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Director")]
public class AdmissionCampaignsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AdmissionCampaignsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdmissionCampaignDto>>> GetAll()
    {
        var campaigns = await BaseQuery().OrderByDescending(c => c.StartDate).ToListAsync();
        return Ok(campaigns.Select(ToDto));
    }

    /// Consultation publique (sans authentification) : campagnes actuellement ouvertes et les programmes
    /// pour lesquels un quota a été défini, pour peupler le formulaire de candidature public.
    [HttpGet("open")]
    [AllowAnonymous]
    public async Task<ActionResult<List<OpenAdmissionCampaignDto>>> GetOpen()
    {
        var now = DateTime.UtcNow;

        var campaigns = await _context.AdmissionCampaigns
            .Include(c => c.Quotas).ThenInclude(q => q.Program)
            .Where(c => c.StartDate <= now && c.EndDate >= now)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var result = campaigns.Select(c => new OpenAdmissionCampaignDto(
            c.Id, c.Name,
            c.Quotas.Select(q => new OpenCampaignProgramDto(q.ProgramId, q.Program.Name)).ToList()));

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdmissionCampaignDto>> Create(CreateAdmissionCampaignRequest request)
    {
        if (request.EndDate.AsUtc() <= request.StartDate.AsUtc())
            return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        var campaign = new AdmissionCampaign
        {
            Name = request.Name,
            AcademicYearId = request.AcademicYearId,
            StartDate = request.StartDate.AsUtc(),
            EndDate = request.EndDate.AsUtc()
        };

        _context.AdmissionCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(c => c.Id == campaign.Id);
        return Ok(ToDto(full));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdmissionCampaignDto>> Update(Guid id, UpdateAdmissionCampaignRequest request)
    {
        if (request.EndDate.AsUtc() <= request.StartDate.AsUtc())
            return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

        var campaign = await _context.AdmissionCampaigns.FindAsync(id);
        if (campaign is null) return NotFound();

        campaign.Name = request.Name;
        campaign.StartDate = request.StartDate.AsUtc();
        campaign.EndDate = request.EndDate.AsUtc();

        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(c => c.Id == id);
        return Ok(ToDto(full));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var campaign = await _context.AdmissionCampaigns.FindAsync(id);
        if (campaign is null) return NotFound();

        var hasApplicants = await _context.StudentApplicants.AnyAsync(a => a.AdmissionCampaignId == id);
        if (hasApplicants)
            return Conflict(new { message = "Cette campagne a des candidatures rattachées et ne peut pas être supprimée." });

        var quotas = await _context.AdmissionCampaignQuotas.Where(q => q.AdmissionCampaignId == id).ToListAsync();
        _context.AdmissionCampaignQuotas.RemoveRange(quotas);
        _context.AdmissionCampaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Définit (ou met à jour) le quota d'un programme pour cette campagne.
    [HttpPost("{id:guid}/quotas")]
    public async Task<ActionResult<AdmissionCampaignDto>> SetQuota(Guid id, SetCampaignQuotaRequest request)
    {
        var campaign = await _context.AdmissionCampaigns.FindAsync(id);
        if (campaign is null) return NotFound();

        var program = await _context.AcademicPrograms.FindAsync(request.ProgramId);
        if (program is null) return NotFound(new { message = "Programme introuvable." });

        var existing = await _context.AdmissionCampaignQuotas
            .FirstOrDefaultAsync(q => q.AdmissionCampaignId == id && q.ProgramId == request.ProgramId);

        if (existing is null)
        {
            _context.AdmissionCampaignQuotas.Add(new AdmissionCampaignQuota
            {
                AdmissionCampaignId = id,
                ProgramId = request.ProgramId,
                Quota = request.Quota
            });
        }
        else
        {
            existing.Quota = request.Quota;
        }

        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(c => c.Id == id);
        return Ok(ToDto(full));
    }

    [HttpDelete("quotas/{quotaId:guid}")]
    public async Task<IActionResult> DeleteQuota(Guid quotaId)
    {
        var quota = await _context.AdmissionCampaignQuotas.FindAsync(quotaId);
        if (quota is null) return NotFound();

        _context.AdmissionCampaignQuotas.Remove(quota);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<AdmissionCampaign> BaseQuery() => _context.AdmissionCampaigns
        .Include(c => c.AcademicYear)
        .Include(c => c.Quotas).ThenInclude(q => q.Program)
        .Include(c => c.Applicants);

    private static AdmissionCampaignDto ToDto(AdmissionCampaign c)
    {
        var now = DateTime.UtcNow;
        return new AdmissionCampaignDto(
            c.Id, c.Name, c.AcademicYearId, c.AcademicYear.Name,
            c.StartDate, c.EndDate, c.StartDate <= now && c.EndDate >= now,
            c.Applicants.Count,
            c.Quotas.Select(q =>
            {
                var used = c.Applicants.Count(a =>
                    a.ProgramId == q.ProgramId &&
                    a.Status is AdmissionStatus.Accepted or AdmissionStatus.Enrolled);
                return new AdmissionCampaignQuotaDto(q.Id, q.ProgramId, q.Program.Name, q.Quota, used, q.Quota - used);
            }).ToList());
    }
}
