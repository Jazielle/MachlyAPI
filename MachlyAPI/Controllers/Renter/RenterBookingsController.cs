using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Bookings;
using MachlyAPI.Models;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers.Renter;

[ApiController]
[Route("api/renter/bookings")]
[Authorize(Roles = "RENTER")]
public class RenterBookingsController : ControllerBase
{
    private readonly MongoDbService _mongoDb;

    public RenterBookingsController(MongoDbService mongoDb)
    {
        _mongoDb = mongoDb;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpGet]
    public async Task<ActionResult<List<BookingDetailDto>>> GetMyBookings()
    {
        var renterId = GetUserId();

        var bookings = await _mongoDb.Bookings
            .Find(b => b.RenterId == renterId)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var result = new List<BookingDetailDto>();

        foreach (var booking in bookings)
        {
            var machine = await _mongoDb.Machines.Find(m => m.Id == booking.MachineId).FirstOrDefaultAsync();
            var provider = await _mongoDb.Users.Find(u => u.Id == booking.ProviderId).FirstOrDefaultAsync();
            var renter = await _mongoDb.Users.Find(u => u.Id == renterId).FirstOrDefaultAsync();

            result.Add(new BookingDetailDto
            {
                Id = booking.Id!,
                MachineId = booking.MachineId,
                MachineTitle = machine?.Title ?? "",
                MachinePhoto = machine?.Photos.FirstOrDefault(),
                RenterId = renterId,
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
}
