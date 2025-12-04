using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MachlyAPI.Models;

public class Review
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("machineId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string MachineId { get; set; } = string.Empty;

    [BsonElement("renterId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RenterId { get; set; } = string.Empty;

    [BsonElement("bookingId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string BookingId { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; } // 1-5

    [BsonElement("comment")]
    public string? Comment { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
