using System.ComponentModel.DataAnnotations;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lastname is required")]
    [StringLength(50, MinimumLength = 2)]
    public string Lastname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }
}
