using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Auth;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly FileUploadService _fileUpload;

    public UsersController(MongoDbService mongoDb, FileUploadService fileUpload)
    {
        _mongoDb = mongoDb;
        _fileUpload = fileUpload;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetProfile(string id)
    {
        // Permitir que un usuario vea su propio perfil o un admin vea cualquiera
        // O si es público (ej. ver perfil del proveedor), habría que ajustar. 
        // Por ahora asumimos que es para ver el propio perfil o el de un proveedor asociado a una máquina.
        
        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new UserDto
        {
            Id = user.Id!,
            Name = user.Name,
            Lastname = user.Lastname,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            PhotoUrl = user.PhotoUrl,
            Verified = user.Verified,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateProfile(string id, [FromBody] UpdateUserDto dto)
    {
        var currentUserId = GetUserId();
        if (currentUserId != id && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Actualizar campos permitidos
        user.Name = dto.Name;
        user.Lastname = dto.Lastname;
        user.Phone = dto.Phone;
        
        // No permitimos cambiar email o rol aquí por seguridad básica sin verificación extra

        await _mongoDb.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new UserDto
        {
            Id = user.Id!,
            Name = user.Name,
            Lastname = user.Lastname,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            PhotoUrl = user.PhotoUrl,
            Verified = user.Verified,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("{id}/photo")]
    public async Task<ActionResult<string>> UploadPhoto(string id, IFormFile photo)
    {
        var currentUserId = GetUserId();
        if (currentUserId != id && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var photoUrl = await _fileUpload.SaveFileAsync(photo, "users");

        user.PhotoUrl = photoUrl;
        await _mongoDb.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new { url = photoUrl });
    }

    [HttpPut("{id}/role")]
    public async Task<ActionResult<UserRoleResponseDto>> UpdateUserRole(string id, [FromBody] UpdateRoleDto dto)
    {
        var currentUserId = GetUserId();
        
        // Solo el mismo usuario puede cambiar su propio rol
        if (currentUserId != id)
        {
            return Forbid();
        }

        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Validación: No permite cambiar a ADMIN (0)
        if (dto.Role == (int)UserRole.ADMIN)
        {
            return BadRequest(new { message = "Cannot change role to ADMIN" });
        }

        // Validación: Solo permite cambiar de RENTER (2) a PROVIDER (1)
        if (user.Role == UserRole.RENTER && dto.Role == (int)UserRole.PROVIDER)
        {
            user.Role = UserRole.PROVIDER;
        }
        else if (user.Role == UserRole.PROVIDER && dto.Role == (int)UserRole.RENTER)
        {
            // No permite cambiar de PROVIDER a RENTER
            return BadRequest(new { message = "Cannot change role from PROVIDER to RENTER" });
        }
        else if (user.Role == UserRole.RENTER && dto.Role == (int)UserRole.RENTER)
        {
            // Ya es RENTER, no hay cambio
            return BadRequest(new { message = "User is already a RENTER" });
        }
        else if (user.Role == UserRole.PROVIDER && dto.Role == (int)UserRole.PROVIDER)
        {
            // Ya es PROVIDER, no hay cambio
            return BadRequest(new { message = "User is already a PROVIDER" });
        }
        else
        {
            return BadRequest(new { message = "Invalid role change" });
        }

        await _mongoDb.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new UserRoleResponseDto
        {
            Id = user.Id!,
            Name = user.Name,
            Lastname = user.Lastname,
            Email = user.Email,
            Phone = user.Phone,
            Role = (int)user.Role,
            PhotoUrl = user.PhotoUrl,
            Verified = user.Verified,
            CreatedAt = user.CreatedAt
        });
    }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
