using MachlyAPI.Models.Enums;

namespace MachlyAPI.DTOs.Machines;

public class MachineDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MachineCategory Category { get; set; }
    public CategoryDataDto CategoryData { get; set; } = new();
    public List<string> Photos { get; set; } = new();
    public LocationDto Location { get; set; } = new();
    public double Rating { get; set; }
    public int ReviewsCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MachineListDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MachineCategory Category { get; set; }
    public decimal TarifaBase { get; set; }
    public string? MainPhoto { get; set; }
    public LocationDto Location { get; set; } = new();
    public double Rating { get; set; }
    public int ReviewsCount { get; set; }
    public double? Distance { get; set; } // En kilómetros
}
