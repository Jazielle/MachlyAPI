using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MachlyAPI.DTOs.Reviews;
using MachlyAPI.Models;
using MachlyAPI.Models.Enums;
using MachlyAPI.Services;

namespace MachlyAPI.Controllers;

[ApiController]
[Route("api/machines/{machineId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly MongoDbService _mongoDb;

    public ReviewsController(MongoDbService mongoDb)
    {
        _mongoDb = mongoDb;
    }

    private string GetUserId() => User.FindFirst("userId")?.Value ?? "";

    [HttpGet]
    public async Task<ActionResult<List<ReviewDto>>> GetReviews(string machineId)
    {
        var reviews = await _mongoDb.Reviews
            .Find(r => r.MachineId == machineId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<ReviewDto>();

        foreach (var review in reviews)
        {
            var renter = await _mongoDb.Users.Find(u => u.Id == review.RenterId).FirstOrDefaultAsync();

            result.Add(new ReviewDto
            {
                Id = review.Id!,
                RenterName = renter?.Name ?? "Anonymous",
                RenterPhoto = renter?.PhotoUrl,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "RENTER")]
    public async Task<ActionResult<ReviewDto>> CreateReview(string machineId, [FromBody] CreateReviewDto dto)
    {
        var renterId = GetUserId();

        // Verificar que la reserva existe y está finalizada
        var booking = await _mongoDb.Bookings.Find(b => b.Id == dto.BookingId && b.RenterId == renterId).FirstOrDefaultAsync();
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        if (booking.Status != BookingStatus.Finished)
        {
            return BadRequest(new { message = "Can only review finished bookings" });
        }

        if (booking.MachineId != machineId)
        {
            return BadRequest(new { message = "Booking does not match machine" });
        }

        // Verificar que no haya review duplicado
        var existingReview = await _mongoDb.Reviews.Find(r => r.BookingId == dto.BookingId).FirstOrDefaultAsync();
        if (existingReview != null)
        {
            return BadRequest(new { message = "Review already exists for this booking" });
        }

        // Crear review
        var review = new Review
        {
            MachineId = machineId,
            RenterId = renterId,
            BookingId = dto.BookingId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _mongoDb.Reviews.InsertOneAsync(review);

        // Actualizar rating de la máquina
        var allReviews = await _mongoDb.Reviews.Find(r => r.MachineId == machineId).ToListAsync();
        var avgRating = allReviews.Average(r => r.Rating);

        var machine = await _mongoDb.Machines.Find(m => m.Id == machineId).FirstOrDefaultAsync();
        if (machine != null)
        {
            machine.Rating = avgRating;
            machine.ReviewsCount = allReviews.Count;
            await _mongoDb.Machines.ReplaceOneAsync(m => m.Id == machineId, machine);
        }

        var renter = await _mongoDb.Users.Find(u => u.Id == renterId).FirstOrDefaultAsync();

        return CreatedAtAction(nameof(GetReviews), new { machineId }, new ReviewDto
        {
            Id = review.Id!,
            RenterName = renter?.Name ?? "Anonymous",
            RenterPhoto = renter?.PhotoUrl,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }
}
