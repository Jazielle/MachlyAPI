namespace MachlyAPI.DTOs.Auth;

public class UserRoleResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int Role { get; set; }  // int: 0=ADMIN, 1=PROVIDER, 2=RENTER
    public string? PhotoUrl { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAt { get; set; }
}
