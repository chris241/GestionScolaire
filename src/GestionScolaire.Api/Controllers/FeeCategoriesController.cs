using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.FeeCategories;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeeCategoriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FeeCategoriesController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<FeeCategoryDto>>> GetAll()
    {
        var categories = await _context.FeeCategories
            .OrderBy(c => c.Name)
            .Select(c => new FeeCategoryDto(c.Id, c.Name, c.Description, c.IsMandatory))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<FeeCategoryDto>> Create(CreateFeeCategoryRequest request)
    {
        var category = new FeeCategory
        {
            Name = request.Name,
            Description = request.Description,
            IsMandatory = request.IsMandatory,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.FeeCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new FeeCategoryDto(category.Id, category.Name, category.Description, category.IsMandatory));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<FeeCategoryDto>> Update(Guid id, UpdateFeeCategoryRequest request)
    {
        var category = await _context.FeeCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsMandatory = request.IsMandatory;
        await _context.SaveChangesAsync();

        return Ok(new FeeCategoryDto(category.Id, category.Name, category.Description, category.IsMandatory));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.FeeCategories.FindAsync(id);
        if (category is null) return NotFound();

        _context.FeeCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
