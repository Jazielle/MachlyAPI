using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.Models;

public class Machine
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("providerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProviderId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("category")]
    [BsonRepresentation(BsonType.String)]
    public MachineCategory Category { get; set; }

    [BsonElement("categoryData")]
    public CategoryData CategoryData { get; set; } = new();

    [BsonElement("photos")]
    public List<string> Photos { get; set; } = new();

    [BsonElement("location")]
    public GeoLocation Location { get; set; } = new();

    [BsonElement("calendar")]
    public List<CalendarEntry> Calendar { get; set; } = new();

    [BsonElement("rating")]
    public double Rating { get; set; } = 0;

    [BsonElement("reviewsCount")]
    public int ReviewsCount { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

public class CategoryData
{
    [BsonElement("hectareas")]
    public double? Hectareas { get; set; }

    [BsonElement("toneladas")]
    public double? Toneladas { get; set; }

    [BsonElement("kilometros")]
    public double? Kilometros { get; set; }

    [BsonElement("tarifaBase")]
    public decimal TarifaBase { get; set; }

    [BsonElement("tarifaOperador")]
    public decimal? TarifaOperador { get; set; }

    [BsonElement("withOperator")]
    public bool WithOperator { get; set; } = false;
}

public class GeoLocation
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = new double[2]; // [longitude, latitude]

    [BsonIgnore]
    public double Longitude
    {
        get => Coordinates.Length > 0 ? Coordinates[0] : 0;
        set
        {
            if (Coordinates.Length < 2) Coordinates = new double[2];
            Coordinates[0] = value;
        }
    }

    [BsonIgnore]
    public double Latitude
    {
        get => Coordinates.Length > 1 ? Coordinates[1] : 0;
        set
        {
            if (Coordinates.Length < 2) Coordinates = new double[2];
            Coordinates[1] = value;
        }
    }
}

public class CalendarEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("start")]
    public DateTime Start { get; set; }

    [BsonElement("end")]
    public DateTime End { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "blocked"; // blocked, reserved

    [BsonElement("bookingId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? BookingId { get; set; }
}
