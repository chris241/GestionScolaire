using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Courses;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public CoursesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetAll([FromQuery] Guid? programId)
    {
        var query = _context.Courses
            .Include(c => c.Subject)
            .Include(c => c.Program)
            .Include(c => c.Topics)
            .AsQueryable();

        if (programId.HasValue)
            query = query.Where(c => c.ProgramId == programId.Value);

        var courses = await query.OrderBy(c => c.Name).ToListAsync();

        return Ok(courses.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Subject)
            .Include(c => c.Program)
            .Include(c => c.Topics)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();

        return Ok(ToDto(course));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest request)
    {
        var subject = await _context.Subjects.FindAsync(request.SubjectId);
        if (subject is null) return NotFound(new { message = "Matière introuvable." });

        var program = await _context.AcademicPrograms.FindAsync(request.ProgramId);
        if (program is null) return NotFound(new { message = "Programme introuvable." });

        var course = new Course
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            SubjectId = request.SubjectId,
            ProgramId = request.ProgramId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return Ok(new CourseDto(course.Id, course.Name, course.Code, course.Description, subject.Id, subject.Name, program.Id, program.Name, new List<TopicDto>()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<CourseDto>> Update(Guid id, UpdateCourseRequest request)
    {
        var course = await _context.Courses
            .Include(c => c.Subject)
            .Include(c => c.Program)
            .Include(c => c.Topics)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();

        course.Name = request.Name;
        course.Code = request.Code;
        course.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(ToDto(course));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course is null) return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/topics")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<TopicDto>> AddTopic(Guid id, CreateTopicRequest request)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var topic = new Topic
        {
            CourseId = id,
            Name = request.Name,
            Description = request.Description,
            Order = request.Order
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        return Ok(new TopicDto(topic.Id, topic.Name, topic.Description, topic.Order));
    }

    [HttpPut("topics/{topicId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<TopicDto>> UpdateTopic(Guid topicId, UpdateTopicRequest request)
    {
        var topic = await _context.Topics.FindAsync(topicId);
        if (topic is null) return NotFound();

        topic.Name = request.Name;
        topic.Description = request.Description;
        topic.Order = request.Order;

        await _context.SaveChangesAsync();

        return Ok(new TopicDto(topic.Id, topic.Name, topic.Description, topic.Order));
    }

    [HttpDelete("topics/{topicId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<IActionResult> DeleteTopic(Guid topicId)
    {
        var topic = await _context.Topics.FindAsync(topicId);
        if (topic is null) return NotFound();

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static CourseDto ToDto(Course c) => new(
        c.Id, c.Name, c.Code, c.Description,
        c.SubjectId, c.Subject.Name,
        c.ProgramId, c.Program.Name,
        c.Topics.OrderBy(t => t.Order).Select(t => new TopicDto(t.Id, t.Name, t.Description, t.Order)).ToList());
}
