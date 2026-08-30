using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Rooms;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RoomsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll()
    {
        var rooms = await _context.Rooms
            .OrderBy(r => r.Name)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.Building))
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomRequest request)
    {
        var room = new Room
        {
            Name = request.Name,
            Capacity = request.Capacity,
            Building = request.Building,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return Ok(new RoomDto(room.Id, room.Name, room.Capacity, room.Building));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, UpdateRoomRequest request)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        room.Name = request.Name;
        room.Capacity = request.Capacity;
        room.Building = request.Building;

        await _context.SaveChangesAsync();

        return Ok(new RoomDto(room.Id, room.Name, room.Capacity, room.Building));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
