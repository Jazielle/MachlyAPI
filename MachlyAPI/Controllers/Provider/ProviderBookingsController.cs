using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Bookings;
using MachlyAPI.Models;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers.Provider;

[ApiController]
[Route("api/provider/bookings")]
[Authorize(Roles = "PROVIDER")]
public class ProviderBookingsController : ControllerBase
{
    private readonly MongoDbService _mongoDb;

    public ProviderBookingsController(MongoDbService mongoDb)
    {
        _mongoDb = mongoDb;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpGet]
    public async Task<ActionResult<List<BookingDetailDto>>> GetMyBookings()
    {
        var providerId = GetUserId();

        var bookings = await _mongoDb.Bookings
            .Find(b => b.ProviderId == providerId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var result = new List<BookingDetailDto>();

        foreach (var booking in bookings)
        {
            var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
            var renter = await _mongoDb.Users.Find(u => u.Id == booking.RenterId).FirstOrDefaultAsync();
            var provider = await _mongoDb.Users.Find(u => u.Id == providerId).FirstOrDefaultAsync();

            result.Add(new BookingDetailDto
            {
                Id = booking.Id!,
                MachineId = booking.MachineId,
                MachineTitle = machine?.Title ?? "",
                MachinePhoto = machine?.Photos.FirstOrDefault(),
                RenterId = booking.RenterId,
                RenterName = renter?.Name ?? "",
                ProviderId = providerId,
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
}
