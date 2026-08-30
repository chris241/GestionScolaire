using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Schools;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchoolsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SchoolsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<SchoolDto>>> GetAll()
    {
        if (_currentUser.UserId is null) return Forbid();

        IQueryable<School> query;
        if (_currentUser.Role == "Director")
        {
            query = _context.Schools.Where(s => s.DirectorId == _currentUser.UserId);
        }
        else if (_currentUser.Role == "Teacher")
        {
            var teacher = await _context.Teachers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.UserId == _currentUser.UserId);
            if (teacher is null) return Ok(new List<SchoolDto>());

            query = _context.Schools.Where(s => s.Teachers.Any(ts => ts.TeacherId == teacher.Id));
        }
        else
        {
            return Forbid();
        }

        var schools = await query
            .OrderBy(s => s.Name)
            .Select(s => new SchoolDto(s.Id, s.Name, s.Address, s.Currency, s.DefaultMaxScore, s.DirectorId, s.IsActive))
            .ToListAsync();

        return Ok(schools);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<SchoolDto>> Create(CreateSchoolRequest request)
    {
        var school = new School
        {
            Name = request.Name,
            Address = request.Address,
            Currency = request.Currency,
            DefaultMaxScore = request.DefaultMaxScore,
            DirectorId = _currentUser.UserId!.Value
        };

        _context.Schools.Add(school);
        await _context.SaveChangesAsync();

        return Ok(new SchoolDto(school.Id, school.Name, school.Address, school.Currency, school.DefaultMaxScore, school.DirectorId, school.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<SchoolDto>> Update(Guid id, UpdateSchoolRequest request)
    {
        var school = await _context.Schools.FirstOrDefaultAsync(s => s.Id == id);
        if (school is null) return NotFound();
        if (school.DirectorId != _currentUser.UserId) return Forbid();

        school.Name = request.Name;
        school.Address = request.Address;
        school.Currency = request.Currency;
        school.DefaultMaxScore = request.DefaultMaxScore;
        await _context.SaveChangesAsync();

        return Ok(new SchoolDto(school.Id, school.Name, school.Address, school.Currency, school.DefaultMaxScore, school.DirectorId, school.IsActive));
    }
}
