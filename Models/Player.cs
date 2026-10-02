using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KerRandoQcm.Models;

public class Player : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("last_name")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("friend_group_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? FriendGroupId { get; set; }

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? TeamId { get; set; }

    [BsonElement("connection_token")]
    public string ConnectionToken { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
