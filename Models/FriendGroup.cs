using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KerRandoQcm.Models;

public class FriendGroup : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
