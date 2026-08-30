using System.Security.Claims;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
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

        var response = await BuildAuthResponseAsync(user);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        var response = await BuildAuthResponseAsync(user);

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

        var response = await BuildAuthResponseAsync(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("switch-school")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> SwitchSchool(SwitchSchoolRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        var hasAccess = user.Role switch
        {
            UserRole.Director => await _context.Schools.AnyAsync(s => s.Id == request.SchoolId && s.DirectorId == user.Id),
            UserRole.Teacher => await HasTeacherAccessAsync(user.Id, request.SchoolId),
            _ => false
        };

        if (!hasAccess)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous n'avez pas accès à cette école." });

        user.LastActiveSchoolId = request.SchoolId;

        var response = await BuildAuthResponseAsync(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _context.SaveChangesAsync();

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        var accessibleSchools = await GetAccessibleSchoolsAsync(user);
        var schoolId = _currentUserService.SchoolId;
        var schoolName = await ResolveSchoolNameAsync(schoolId, accessibleSchools);

        return Ok(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            schoolId, schoolName, accessibleSchools));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task<bool> HasTeacherAccessAsync(Guid userId, Guid schoolId)
    {
        var teacher = await _context.Teachers.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.UserId == userId);
        if (teacher is null) return false;

        return await _context.TeacherSchools.AnyAsync(ts => ts.TeacherId == teacher.Id && ts.SchoolId == schoolId);
    }

    private async Task<List<SchoolSummaryDto>> GetAccessibleSchoolsAsync(User user)
    {
        if (user.Role == UserRole.Director)
        {
            return await _context.Schools
                .Where(s => s.DirectorId == user.Id)
                .OrderBy(s => s.Name)
                .Select(s => new SchoolSummaryDto(s.Id, s.Name))
                .ToListAsync();
        }

        if (user.Role == UserRole.Teacher)
        {
            var teacher = await _context.Teachers.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher is null) return new List<SchoolSummaryDto>();

            return await _context.TeacherSchools
                .Where(ts => ts.TeacherId == teacher.Id)
                .OrderBy(ts => ts.School.Name)
                .Select(ts => new SchoolSummaryDto(ts.School.Id, ts.School.Name))
                .ToListAsync();
        }

        return new List<SchoolSummaryDto>();
    }

    private async Task<string?> ResolveSchoolNameAsync(Guid? schoolId, List<SchoolSummaryDto> accessibleSchools)
    {
        if (!schoolId.HasValue) return null;

        return accessibleSchools.FirstOrDefault(s => s.Id == schoolId)?.Name
            ?? await _context.Schools.Where(s => s.Id == schoolId).Select(s => s.Name).FirstOrDefaultAsync();
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var accessibleSchools = await GetAccessibleSchoolsAsync(user);
        Guid? schoolId = null;

        if (user.Role is UserRole.Director or UserRole.Teacher)
        {
            schoolId = user.LastActiveSchoolId.HasValue && accessibleSchools.Any(s => s.Id == user.LastActiveSchoolId)
                ? user.LastActiveSchoolId
                : accessibleSchools.FirstOrDefault()?.Id;
            user.LastActiveSchoolId = schoolId;
        }
        else if (user.Role == UserRole.Student)
        {
            schoolId = await _context.Students.IgnoreQueryFilters()
                .Where(s => s.UserId == user.Id)
                .Select(s => (Guid?)s.Class.SchoolId)
                .FirstOrDefaultAsync();
        }

        var schoolName = await ResolveSchoolNameAsync(schoolId, accessibleSchools);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, schoolId);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(),
            schoolId, schoolName, accessibleSchools);

        return new AuthResponse(accessToken, refreshToken, expiresAt, userDto);
    }
}
