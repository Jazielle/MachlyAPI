using System.ComponentModel.DataAnnotations;

namespace MachlyAPI.DTOs.Reviews;

public class CreateReviewDto
{
    [Required]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public string RenterName { get; set; } = string.Empty;
    public string? RenterPhoto { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
