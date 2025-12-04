using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Machines;
using MachlyAPI.Models;
using MachlyAPI.Services;
using System.Security.Claims;

namespace MachlyAPI.Controllers.Provider;

[ApiController]
[Route("api/provider/machines")]
[Authorize(Roles = "PROVIDER")]
public class ProviderMachinesController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly FileUploadService _fileUpload;

    public ProviderMachinesController(MongoDbService mongoDb, FileUploadService fileUpload)
    {
        _mongoDb = mongoDb;
        _fileUpload = fileUpload;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpPost]
    public async Task<ActionResult<MachineDetailDto>> CreateMachine([FromBody] CreateMachineDto dto)
    {
        var providerId = GetUserId();

        var machine = new Machine
        {
            ProviderId = providerId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            CategoryData = new CategoryData
            {
                Hectareas = dto.CategoryData.Hectareas,
                Toneladas = dto.CategoryData.Toneladas,
                Kilometros = dto.CategoryData.Kilometros,
                TarifaBase = dto.CategoryData.TarifaBase,
                TarifaOperador = dto.CategoryData.TarifaOperador,
                WithOperator = dto.CategoryData.WithOperator
            },
            Location = new GeoLocation
            {
                Latitude = dto.Location.Latitude,
                Longitude = dto.Location.Longitude
            },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _mongoDb.Machines.InsertOneAsync(machine);

        var provider = await _mongoDb.Users.Find(u => u.Id == providerId).FirstOrDefaultAsync();

        return CreatedAtAction(nameof(CreateMachine), new { id = machine.Id }, new MachineDetailDto
        {
            Id = machine.Id!,
            ProviderId = machine.ProviderId,
            ProviderName = provider?.Name ?? "",
            Title = machine.Title,
            Description = machine.Description,
            Category = machine.Category,
            CategoryData = new CategoryDataDto
            {
                Hectareas = machine.CategoryData.Hectareas,
                Toneladas = machine.CategoryData.Toneladas,
                Kilometros = machine.CategoryData.Kilometros,
                TarifaBase = machine.CategoryData.TarifaBase,
                TarifaOperador = machine.CategoryData.TarifaOperador,
                WithOperator = machine.CategoryData.WithOperator
            },
            Photos = machine.Photos,
            Location = new LocationDto
            {
                Latitude = machine.Location.Latitude,
                Longitude = machine.Location.Longitude
            },
            Rating = machine.Rating,
            ReviewsCount = machine.ReviewsCount,
            IsActive = machine.IsActive,
            CreatedAt = machine.CreatedAt
        });
    }

    [HttpPost("{id}/photos")]
    public async Task<ActionResult<List<string>>> UploadPhotos(string id, [FromForm] List<IFormFile> photos)
    {
        var providerId = GetUserId();

        var machine = await _mongoDb.Machines.Find(m => m.Id == id && m.ProviderId == providerId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        var urls = await _fileUpload.SaveMultipleFilesAsync(photos, "machines");

        machine.Photos.AddRange(urls);
        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == id, machine);

        return Ok(urls);
    }

    [HttpGet]
    public async Task<ActionResult<List<MachineDetailDto>>> GetMyMachines()
    {
        var providerId = GetUserId();

        var machines = await _mongoDb.Machines
            .Find(m => m.ProviderId == providerId)
            .ToListAsync();

        var provider = await _mongoDb.Users.Find(u => u.Id == providerId).FirstOrDefaultAsync();

        var result = machines.Select(m => new MachineDetailDto
        {
            Id = m.Id!,
            ProviderId = m.ProviderId,
            ProviderName = provider?.Name ?? "",
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
        }).ToList();

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateMachine(string id, [FromBody] CreateMachineDto dto)
    {
        var providerId = GetUserId();

        var machine = await _mongoDb.Machines.Find(m => m.Id == id && m.ProviderId == providerId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        machine.Title = dto.Title;
        machine.Description = dto.Description;
        machine.Category = dto.Category;
        machine.CategoryData = new CategoryData
        {
            Hectareas = dto.CategoryData.Hectareas,
            Toneladas = dto.CategoryData.Toneladas,
            Kilometros = dto.CategoryData.Kilometros,
            TarifaBase = dto.CategoryData.TarifaBase,
            TarifaOperador = dto.CategoryData.TarifaOperador,
            WithOperator = dto.CategoryData.WithOperator
        };
        machine.Location = new GeoLocation
        {
            Latitude = dto.Location.Latitude,
            Longitude = dto.Location.Longitude
        };

        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == id, machine);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMachine(string id)
    {
        var providerId = GetUserId();

        var result = await _mongoDb.Machines.DeleteOneAsync(m => m.Id == id && m.ProviderId == providerId);
        if (result.DeletedCount == 0)
        {
            return NotFound(new { message = "Machine not found" });
        }

        return NoContent();
    }
    [HttpPost("{id}/calendar")]
    public async Task<ActionResult> BlockDates(string id, [FromBody] BlockDatesDto dto)
    {
        var providerId = GetUserId();

        var machine = await _mongoDb.Machines.Find(m => m.Id == id && m.ProviderId == providerId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        // Verificar solapamiento
        var hasConflict = machine.Calendar.Any(c =>
            (dto.Start >= c.Start && dto.Start < c.End) ||
            (dto.End > c.Start && dto.End <= c.End) ||
            (dto.Start <= c.Start && dto.End >= c.End)
        );

        if (hasConflict)
        {
            return BadRequest(new { message = "Dates already blocked or reserved" });
        }

        var entry = new CalendarEntry
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Start = dto.Start,
            End = dto.End,
            Status = "blocked"
        };

        machine.Calendar.Add(entry);
        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == id, machine);

        return Ok(entry);
    }

    [HttpDelete("{id}/calendar/{entryId}")]
    public async Task<ActionResult> UnblockDates(string id, string entryId)
    {
        var providerId = GetUserId();

        var machine = await _mongoDb.Machines.Find(m => m.Id == id && m.ProviderId == providerId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        var entry = machine.Calendar.FirstOrDefault(c => c.Id == entryId);
        if (entry == null)
        {
            return NotFound(new { message = "Calendar entry not found" });
        }

        if (entry.Status == "reserved")
        {
            return BadRequest(new { message = "Cannot delete a reservation. Cancel the booking instead." });
        }

        machine.Calendar.Remove(entry);
        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == id, machine);

        return NoContent();
    }
}

public class BlockDatesDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
