using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MachlyAPI.Models.Enums;

namespace MachlyAPI.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("lastname")]
    public string Lastname { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.RENTER;

    [BsonElement("photoUrl")]
    public string? PhotoUrl { get; set; }

    [BsonElement("verified")]
    public bool Verified { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
