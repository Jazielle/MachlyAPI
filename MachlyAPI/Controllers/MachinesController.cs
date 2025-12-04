using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Machines;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers;

[ApiController]
[Route("api/machines")]
public class MachinesController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly GeoService _geoService;

    public MachinesController(MongoDbService mongoDb, GeoService geoService)
    {
        _mongoDb = mongoDb;
        _geoService = geoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MachineDetailDto>>> SearchMachines(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double radius = 50, // Radio por defecto 50km
        [FromQuery] MachineCategory? category = null)
    {
        // 1. Filtrar por categoría y estado activo
        var filterBuilder = Builders<Machine>.Filter;
        var filter = filterBuilder.Eq(m => m.IsActive, true);

        if (category.HasValue)
        {
            filter &= filterBuilder.Eq(m => m.Category, category.Value);
        }

        var machines = await _mongoDb.Machines.Find(filter).ToListAsync();

        // 2. Filtrar por ubicación (Geoespacial) si se proporcionan coordenadas
        if (lat.HasValue && lng.HasValue)
        {
            // Nota: Idealmente usaríamos índices geoespaciales de MongoDB ($near),
            // pero para simplificar y mantener compatibilidad con el modelo actual
            // haremos el filtrado en memoria usando el GeoService, ya que el volumen de datos no es masivo aún.
            machines = machines.Where(m => 
                _geoService.CalculateDistance(lat.Value, lng.Value, m.Location.Latitude, m.Location.Longitude) <= radius
            ).ToList();
        }

        // 3. Mapear a DTO
        var result = new List<MachineDetailDto>();
        foreach (var m in machines)
        {
            var provider = await _mongoDb.Users.Find(u => u.Id == m.ProviderId).FirstOrDefaultAsync();
            
            result.Add(MapToDto(m, provider));
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MachineDetailDto>> GetMachine(string id)
    {
        var machine = await _mongoDb.Machines.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        var provider = await _mongoDb.Users.Find(u => u.Id == machine.ProviderId).FirstOrDefaultAsync();

        return Ok(MapToDto(machine, provider));
    }

    [HttpGet("{id}/calendar")]
    public async Task<ActionResult<List<CalendarEntry>>> GetMachineCalendar(string id)
    {
        var machine = await _mongoDb.Machines.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        // Retornar solo fechas futuras o actuales
        var today = DateTime.UtcNow.Date;
        var activeCalendar = machine.Calendar.Where(c => c.End >= today).ToList();

        return Ok(activeCalendar);
    }

    [HttpGet("disponibilidad")]
    public async Task<ActionResult<bool>> CheckAvailability(
        [FromQuery] string machineId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        var machine = await _mongoDb.Machines.Find(m => m.Id == machineId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        var isBusy = machine.Calendar.Any(c =>
            (start >= c.Start && start < c.End) ||
            (end > c.Start && end <= c.End) ||
            (start <= c.Start && end >= c.End)
        );

        return Ok(!isBusy);
    }

    private MachineDetailDto MapToDto(Machine m, User? provider)
    {
        return new MachineDetailDto
        {
            Id = m.Id!,
            ProviderId = m.ProviderId,
            ProviderName = provider?.Name ?? "Unknown Provider",
            Title = m.Title,
            Description = m.Description,
            Category = m.Category,
            CategoryData = new CategoryDataDto
            {
                Hectareas = m.CategoryData.Hectareas,
                Toneladas = m.CategoryData.Toneladas,
                Kilometros = m.CategoryData.Kilometros,
                TarifaBase = m.CategoryData.TarifaBase,
                TarifaOperador = m.CategoryData.TarifaOperador,
                WithOperator = m.CategoryData.WithOperator
            },
            Photos = m.Photos,
            Location = new LocationDto
            {
                Latitude = m.Location.Latitude,
                Longitude = m.Location.Longitude
            },
            Rating = m.Rating,
            ReviewsCount = m.ReviewsCount,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt
        };
    }
}
