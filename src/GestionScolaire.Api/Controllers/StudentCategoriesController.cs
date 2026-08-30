using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.StudentCategories;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentCategoriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StudentCategoriesController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentCategoryDto>>> GetAll()
    {
        var categories = await _context.StudentCategories
            .OrderBy(c => c.Name)
            .Select(c => new StudentCategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentCategoryDto>> Create(CreateStudentCategoryRequest request)
    {
        var category = new StudentCategory { Name = request.Name, Description = request.Description, SchoolId = _currentUser.SchoolId!.Value };

        _context.StudentCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new StudentCategoryDto(category.Id, category.Name, category.Description));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentCategoryDto>> Update(Guid id, UpdateStudentCategoryRequest request)
    {
        var category = await _context.StudentCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        await _context.SaveChangesAsync();

        return Ok(new StudentCategoryDto(category.Id, category.Name, category.Description));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.StudentCategories.FindAsync(id);
        if (category is null) return NotFound();

        _context.StudentCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
