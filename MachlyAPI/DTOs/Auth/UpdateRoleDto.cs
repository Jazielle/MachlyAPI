using System.ComponentModel.DataAnnotations;

namespace MachlyAPI.DTOs.Auth;

public class UpdateRoleDto
{
    [Required]
    [Range(0, 2, ErrorMessage = "Role must be 0 (ADMIN), 1 (PROVIDER), or 2 (RENTER)")]
    public int Role { get; set; }
}
