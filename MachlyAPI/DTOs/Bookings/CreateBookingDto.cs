using System.ComponentModel.DataAnnotations;

namespace MachlyAPI.DTOs.Bookings;

public class CreateBookingDto
{
    [Required]
    public string MachineId { get; set; } = string.Empty;

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    public double? Quantity { get; set; } // Para hectáreas, toneladas, km
}

public class BookingDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string MachineTitle { get; set; } = string.Empty;
    public string? MachinePhoto { get; set; }
    public string RenterId { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public CheckDataDto? Checkin { get; set; }
    public CheckDataDto? Checkout { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CheckDataDto
{
    public List<string> Photos { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}
