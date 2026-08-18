using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using GestionScolaire.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return BadRequest(new { message = "Rôle invalide. Valeurs autorisées : Director, Teacher, Parent." });

        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict(new { message = "Un compte existe déjà avec cet email." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        var response = BuildAuthResponse(user);

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Session expirée, veuillez vous reconnecter." });

        var response = BuildAuthResponse(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return NotFound();

        return Ok(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString()));
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());

        return new AuthResponse(accessToken, refreshToken, expiresAt, userDto);
    }
}
