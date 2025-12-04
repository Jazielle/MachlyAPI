using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Auth;
using MachlyAPI.Models;
using MachlyAPI.Services;
using BCrypt.Net;

namespace MachlyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly JwtService _jwtService;

    public AuthController(MongoDbService mongoDb, JwtService jwtService)
    {
        _mongoDb = mongoDb;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        // Verificar si el email ya existe
        var existingUser = await _mongoDb.Users
            .Find(u => u.Email == dto.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already registered" });
        }

        // Crear nuevo usuario
        var user = new User
        {
            Name = dto.Name,
            Lastname = dto.Lastname,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone,
            Role = dto.Role,
            Verified = dto.Role == Models.Enums.UserRole.RENTER, // Auto-verificar renters
            CreatedAt = DateTime.UtcNow
        };

        await _mongoDb.Users.InsertOneAsync(user);

        // Generar token
        var token = _jwtService.GenerateToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            UserId = user.Id!,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Verified = user.Verified,
            PhotoUrl = user.PhotoUrl
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        // Buscar usuario
        var user = await _mongoDb.Users
            .Find(u => u.Email == dto.Email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Verificar contraseña
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Generar token
        var token = _jwtService.GenerateToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            UserId = user.Id!,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Verified = user.Verified,
            PhotoUrl = user.PhotoUrl
        });
    }
}
