using MachlyAPI.Models.Enums;

namespace MachlyAPI.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool Verified { get; set; }
    public string? PhotoUrl { get; set; }
}
