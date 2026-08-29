using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Rooms;

public record RoomDto(Guid Id, string Name, int Capacity, string? Building);

public record CreateRoomRequest(
    [Required] string Name,
    [Required] int Capacity,
    string? Building
);

public record UpdateRoomRequest(
    [Required] string Name,
    [Required] int Capacity,
    string? Building
);
