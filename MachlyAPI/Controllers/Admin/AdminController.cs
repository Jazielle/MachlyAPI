using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Admin;
using MachlyAPI.DTOs.Auth;
using MachlyAPI.DTOs.Bookings;
using MachlyAPI.DTOs.Machines;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly MongoDbService _mongoDb;

    public AdminController(MongoDbService mongoDb)
    {
        _mongoDb = mongoDb;
    }

    // ========== DASHBOARD Y ESTADÍSTICAS ==========

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats()
    {
        var totalUsers = await _mongoDb.Users.CountDocumentsAsync(FilterDefinition<User>.Empty);
        var totalProviders = await _mongoDb.Users.CountDocumentsAsync(u => u.Role == UserRole.PROVIDER);
        var totalRenters = await _mongoDb.Users.CountDocumentsAsync(u => u.Role == UserRole.RENTER);
        var totalMachines = await _mongoDb.Machines.CountDocumentsAsync(FilterDefinition<Machine>.Empty);
        var totalBookings = await _mongoDb.Bookings.CountDocumentsAsync(FilterDefinition<Booking>.Empty);
        var pendingVerifications = await _mongoDb.Users.CountDocumentsAsync(u => u.Role == UserRole.PROVIDER && !u.Verified);

        var bookings = await _mongoDb.Bookings.Find(FilterDefinition<Booking>.Empty).ToListAsync();
        var totalRevenue = bookings.Sum(b => b.TotalPrice);

        return Ok(new AdminStatsDto
        {
            TotalUsers = (int)totalUsers,
            TotalProviders = (int)totalProviders,
            TotalRenters = (int)totalRenters,
            TotalMachines = (int)totalMachines,
            TotalBookings = (int)totalBookings,
            PendingVerifications = (int)pendingVerifications,
            TotalRevenue = totalRevenue
        });
    }

    // ========== GESTIÓN DE USUARIOS ==========

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetAllUsers([FromQuery] UserRole? role = null)
    {
        var filter = role.HasValue
            ? Builders<User>.Filter.Eq(u => u.Role, role.Value)
            : FilterDefinition<User>.Empty;

        var users = await _mongoDb.Users.Find(filter).ToListAsync();

        // Ocultar passwordHash
        users.ForEach(u => u.PasswordHash = "***");

        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
    {
        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.PasswordHash = "***";
        return Ok(user);
    }

    [HttpPut("users/{id}/verify")]
    public async Task<ActionResult> VerifyProvider(string id, [FromBody] VerifyProviderDto dto)
    {
        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (user.Role != UserRole.PROVIDER)
        {
            return BadRequest(new { message = "User is not a provider" });
        }

        user.Verified = dto.Verified;
        await _mongoDb.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new { message = $"Provider {(dto.Verified ? "verified" : "unverified")} successfully" });
    }

    [HttpPut("users/{id}/role")]
    public async Task<ActionResult<UserRoleResponseDto>> UpdateUserRoleAsAdmin(string id, [FromBody] UpdateRoleDto dto)
    {
        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Validación: No permite cambiar el rol del último ADMIN
        if (user.Role == UserRole.ADMIN && dto.Role != (int)UserRole.ADMIN)
        {
            var adminCount = await _mongoDb.Users.CountDocumentsAsync(u => u.Role == UserRole.ADMIN);
            if (adminCount <= 1)
            {
                return BadRequest(new { message = "Cannot change the role of the last admin" });
            }
        }

        // Convertir el int a UserRole enum
        user.Role = (UserRole)dto.Role;
        await _mongoDb.Users.ReplaceOneAsync(u => u.Id == id, user);

        return Ok(new UserRoleResponseDto
        {
            Id = user.Id!,
            Name = user.Name,
            Lastname = user.Lastname,
            Email = user.Email,
            Phone = user.Phone,
            Role = (int)user.Role,
            PhotoUrl = user.PhotoUrl,
            Verified = user.Verified,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpDelete("users/{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        // Verificar que no sea el único admin
        var user = await _mongoDb.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user?.Role == UserRole.ADMIN)
        {
            var adminCount = await _mongoDb.Users.CountDocumentsAsync(u => u.Role == UserRole.ADMIN);
            if (adminCount <= 1)
            {
                return BadRequest(new { message = "Cannot delete the last admin" });
            }
        }

        var result = await _mongoDb.Users.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new { message = "User deleted successfully" });
    }

    // ========== GESTIÓN DE MAQUINARIAS ==========

    [HttpGet("machines")]
    public async Task<ActionResult<List<MachineDetailDto>>> GetAllMachines([FromQuery] MachineCategory? category = null)
    {
        var filter = category.HasValue
            ? Builders<Machine>.Filter.Eq(m => m.Category, category.Value)
            : FilterDefinition<Machine>.Empty;

        var machines = await _mongoDb.Machines.Find(filter).ToListAsync();

        var result = new List<MachineDetailDto>();

        foreach (var machine in machines)
        {
            var provider = await _mongoDb.Users.Find(u => u.Id == machine.ProviderId).FirstOrDefaultAsync();

            result.Add(new MachineDetailDto
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

        return Ok(result);
    }

    [HttpDelete("machines/{id}")]
    public async Task<ActionResult> DeleteMachine(string id)
    {
        // Verificar si hay reservas activas
        var activeBookings = await _mongoDb.Bookings
            .Find(b => b.MachineId == id && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .CountDocumentsAsync();

        if (activeBookings > 0)
        {
            return BadRequest(new { message = "Cannot delete machine with active bookings" });
        }

        var result = await _mongoDb.Machines.DeleteOneAsync(m => m.Id == id);
        if (result.DeletedCount == 0)
        {
            return NotFound(new { message = "Machine not found" });
        }

        return Ok(new { message = "Machine deleted successfully" });
    }

    [HttpPut("machines/{id}/toggle")]
    public async Task<ActionResult> ToggleMachineStatus(string id)
    {
        var machine = await _mongoDb.Machines.Find(m => m.Id == id).FirstOrDefaultAsync();
        if (machine == null)
        {
            return NotFound(new { message = "Machine not found" });
        }

        machine.IsActive = !machine.IsActive;
        await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == id, machine);

        return Ok(new { message = $"Machine {(machine.IsActive ? "activated" : "deactivated")} successfully", isActive = machine.IsActive });
    }

    // ========== GESTIÓN DE RESERVAS ==========

    [HttpGet("bookings")]
    public async Task<ActionResult<List<BookingDetailDto>>> GetAllBookings([FromQuery] BookingStatus? status = null)
    {
        var filter = status.HasValue
            ? Builders<Booking>.Filter.Eq(b => b.Status, status.Value)
            : FilterDefinition<Booking>.Empty;

        var bookings = await _mongoDb.Bookings.Find(filter).SortByDescending(b => b.CreatedAt).ToListAsync();

        var result = new List<BookingDetailDto>();

        foreach (var booking in bookings)
        {
            var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
            var renter = await _mongoDb.Users.Find(u => u.Id == booking.RenterId).FirstOrDefaultAsync();
            var provider = await _mongoDb.Users.Find(u => u.Id == booking.ProviderId).FirstOrDefaultAsync();

            result.Add(new BookingDetailDto
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

        return Ok(result);
    }

    [HttpPut("bookings/{id}/cancel")]
    public async Task<ActionResult> CancelBooking(string id)
    {
        var booking = await _mongoDb.Bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        booking.Status = BookingStatus.Canceled;
        await _mongoDb.Bookings.ReplaceOneAsync(b => b.Id == id, booking);

        // Remover del calendario de la máquina
        var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
        if (machine != null)
        {
            machine.Calendar.RemoveAll(c => c.BookingId == id);
            await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == booking.MachineId, machine);
        }

        return Ok(new { message = "Booking canceled successfully" });
    }

    // ========== GESTIÓN DE REVIEWS ==========

    [HttpDelete("reviews/{id}")]
    public async Task<ActionResult> DeleteReview(string id)
    {
        var review = await _mongoDb.Reviews.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        await _mongoDb.Reviews.DeleteOneAsync(r => r.Id == id);

        // Recalcular rating de la máquina
        var allReviews = await _mongoDb.Reviews.Find(r => r.MachineId == review.MachineId).ToListAsync();
        var machine = await _mongoDb.Machines.Find(m => m.Id == review.MachineId).FirstOrDefaultAsync();

        if (machine != null)
        {
            machine.Rating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
            machine.ReviewsCount = allReviews.Count;
            await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == review.MachineId, machine);
        }

        return Ok(new { message = "Review deleted successfully" });
    }
}
