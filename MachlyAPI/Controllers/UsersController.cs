using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Auth;
using MachlyAPI.Models;
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
