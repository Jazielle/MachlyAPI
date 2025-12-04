using System.ComponentModel.DataAnnotations;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.DTOs.Machines;

public class CreateMachineDto
{
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public MachineCategory Category { get; set; }

    [Required]
    public CategoryDataDto CategoryData { get; set; } = new();

    [Required]
    public LocationDto Location { get; set; } = new();
}

public class CategoryDataDto
{
    public double? Hectareas { get; set; }
    public double? Toneladas { get; set; }
    public double? Kilometros { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal TarifaBase { get; set; }

    public decimal? TarifaOperador { get; set; }
    public bool WithOperator { get; set; } = false;
}

public class LocationDto
{
    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }
}
