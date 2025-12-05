using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Bookings;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly MongoDbService _mongoDb;
    private readonly PriceCalculationService _priceCalc;
    private readonly FileUploadService _fileUpload;

    public BookingsController(MongoDbService mongoDb, PriceCalculationService priceCalc, FileUploadService fileUpload)
    {
        _mongoDb = mongoDb;
        _priceCalc = priceCalc;
        _fileUpload = fileUpload;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpPost]
    [Authorize(Roles = "RENTER")]
    public async Task<ActionResult<BookingDetailDto>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var renterId = GetUserId();

        // Verificar que la máquina existe
        var machine = await _mongoDb.Machines.Find(m => m.Id == dto.MachineId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        // Calcular precio
        var totalPrice = _priceCalc.CalculatePrice(machine, dto.Start, dto.End, dto.Quantity);

        // Crear reserva
        var booking = new Booking
        {
            MachineId = dto.MachineId,
            RenterId = renterId,
            ProviderId = machine.ProviderId,
            Start = dto.Start,
            End = dto.End,
            Status = BookingStatus.Pending,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow
        };

        await _mongoDb.Bookings.InsertOneAsync(booking);

        // Agregar al calendario de la máquina
        machine.Calendar.Add(new CalendarEntry
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Start = dto.Start,
            End = dto.End,
            Status = "reserved",
            BookingId = booking.Id
        });

        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == machine.Id, machine);

        // Obtener datos para el DTO
        var renter = await _mongoDb.Users.Find(u => u.Id == renterId).FirstOrDefaultAsync();
        var provider = await _mongoDb.Users.Find(u => u.Id == machine.ProviderId).FirstOrDefaultAsync();

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, new BookingDetailDto
        {
            Id = booking.Id!,
            MachineId = machine.Id!,
            MachineTitle = machine.Title,
            MachinePhoto = machine.Photos.FirstOrDefault(),
            RenterId = renterId,
            RenterName = renter?.Name ?? "",
            ProviderId = machine.ProviderId,
            ProviderName = provider?.Name ?? "",
            Start = booking.Start,
            End = booking.End,
            Status = booking.Status.ToString(),
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt
        });
    }

    [HttpGet("my")]
    [Authorize(Roles = "RENTER")]
    public async Task<ActionResult<List<BookingDetailDto>>> GetMyBookings()
    {
        var renterId = GetUserId();

        var bookings = await _mongoDb.Bookings
            .Find(b => b.RenterId == renterId)
            .ToListAsync();

        var result = new List<BookingDetailDto>();
        foreach (var booking in bookings)
        {
            var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
            var provider = await _mongoDb.Users.Find(u => u.Id == booking.ProviderId).FirstOrDefaultAsync();

            result.Add(new BookingDetailDto
            {
                Id = booking.Id!,
                MachineId = booking.MachineId,
                MachineTitle = machine?.Title ?? "",
                MachinePhoto = machine?.Photos.FirstOrDefault(),
                RenterId = renterId,
                RenterName = "",
                ProviderId = booking.ProviderId,
                ProviderName = provider?.Name ?? "",
                Start = booking.Start,
                End = booking.End,
                Status = booking.Status.ToString(),
                TotalPrice = booking.TotalPrice,
                CreatedAt = booking.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpGet("machine/{machineId}")]
    [Authorize(Roles = "PROVIDER,ADMIN")]
    public async Task<ActionResult<List<BookingDetailDto>>> GetMachineBookings(string machineId)
    {
        var userId = GetUserId();

        // Verificar que la máquina existe
        var machine = await _mongoDb.Machines.Find(m => m.Id == machineId).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        // Verificar que el usuario es el proveedor de la máquina o es admin
        if (machine.ProviderId != userId && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        var bookings = await _mongoDb.Bookings
            .Find(b => b.MachineId == machineId)
            .ToListAsync();

        var result = new List<BookingDetailDto>();
        foreach (var booking in bookings)
        {
            var renter = await _mongoDb.Users.Find(u => u.Id == booking.RenterId).FirstOrDefaultAsync();

            result.Add(new BookingDetailDto
            {
                Id = booking.Id!,
                MachineId = booking.MachineId,
                MachineTitle = machine.Title,
                MachinePhoto = machine.Photos.FirstOrDefault(),
                RenterId = booking.RenterId,
                RenterName = renter?.Name ?? "",
                ProviderId = booking.ProviderId,
                ProviderName = "",
                Start = booking.Start,
                End = booking.End,
                Status = booking.Status.ToString(),
                TotalPrice = booking.TotalPrice,
                CreatedAt = booking.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDetailDto>> GetBooking(string id)
    {
        var booking = await _mongoDb.Bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        var userId = GetUserId();
        if (booking.RenterId != userId && booking.ProviderId != userId && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
        var renter = await _mongoDb.Users.Find(u => u.Id == booking.RenterId).FirstOrDefaultAsync();
        var provider = await _mongoDb.Users.Find(u => u.Id == booking.ProviderId).FirstOrDefaultAsync();

        return Ok(new BookingDetailDto
        {
            Id = booking.Id!,
            MachineId = booking.MachineId,
            MachineTitle = machine?.Title ?? "",
            MachinePhoto = machine?.Photos.FirstOrDefault(),
            RenterId = booking.RenterId,
            RenterName = renter?.Name ?? "",
            ProviderId = booking.ProviderId,
            ProviderName = provider?.Name ?? "",
            Start = booking.Start,
            End = booking.End,
            Status = booking.Status.ToString(),
            TotalPrice = booking.TotalPrice,
            Checkin = booking.Checkin != null ? new CheckDataDto
            {
                Photos = booking.Checkin.Photos,
                Timestamp = booking.Checkin.Timestamp,
                Notes = booking.Checkin.Notes
            } : null,
            Checkout = booking.Checkout != null ? new CheckDataDto
            {
                Photos = booking.Checkout.Photos,
                Timestamp = booking.Checkout.Timestamp,
                Notes = booking.Checkout.Notes
            } : null,
            CreatedAt = booking.CreatedAt
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "PROVIDER,ADMIN")]
    public async Task<ActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto dto)
    {
        var userId = GetUserId();

        var booking = await _mongoDb.Bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        // Verificar que el usuario es el proveedor o admin
        if (booking.ProviderId != userId && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        booking.Status = dto.Status;
        await _mongoDb.Bookings.ReplaceOneAsync(b => b.Id == id, booking);

        return NoContent();
    }

    [HttpPost("{id}/checkin")]
    public async Task<ActionResult> CheckIn(string id, [FromForm] List<IFormFile> photos, [FromForm] string? notes)
    {
        var userId = GetUserId();

        var booking = await _mongoDb.Bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        if (booking.RenterId != userId && booking.ProviderId != userId)
        {
            return Forbid();
        }

        var photoUrls = await _fileUpload.SaveMultipleFilesAsync(photos, "bookings");

        booking.Checkin = new CheckData
        {
            Photos = photoUrls,
            Timestamp = DateTime.UtcNow,
            Notes = notes
        };

        await _mongoDb.Bookings.ReplaceOneAsync(b => b.Id == id, booking);

        return Ok(new { message = "Check-in completed" });
    }

    [HttpPost("{id}/checkout")]
    public async Task<ActionResult> CheckOut(string id, [FromForm] List<IFormFile> photos, [FromForm] string? notes)
    {
        var userId = GetUserId();

        var booking = await _mongoDb.Bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        if (booking.RenterId != userId && booking.ProviderId != userId)
        {
            return Forbid();
        }

        var photoUrls = await _fileUpload.SaveMultipleFilesAsync(photos, "bookings");

        booking.Checkout = new CheckData
        {
            Photos = photoUrls,
            Timestamp = DateTime.UtcNow,
            Notes = notes
        };

        booking.Status = BookingStatus.Finished;

        await _mongoDb.Bookings.ReplaceOneAsync(b => b.Id == id, booking);

        return Ok(new { message = "Check-out completed" });
    }
}

public class UpdateStatusDto
{
    public BookingStatus Status { get; set; }
}
