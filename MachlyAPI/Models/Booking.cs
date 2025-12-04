using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.Models;

public class Booking
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

    [BsonElement("providerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProviderId { get; set; } = string.Empty;

    [BsonElement("start")]
    public DateTime Start { get; set; }

    [BsonElement("end")]
    public DateTime End { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [BsonElement("totalPrice")]
    public decimal TotalPrice { get; set; }

    [BsonElement("checkin")]
    public CheckData? Checkin { get; set; }

    [BsonElement("checkout")]
    public CheckData? Checkout { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CheckData
{
    [BsonElement("photos")]
    public List<string> Photos { get; set; } = new();

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("notes")]
    public string? Notes { get; set; }
}
