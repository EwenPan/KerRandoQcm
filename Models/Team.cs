using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KerRandoQcm.Models;

public class Team : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("current_step")]
    public int CurrentStep { get; set; } = 1;

    [BsonElement("total_score")]
    public int TotalScore { get; set; } = 0;

    [BsonElement("route_step_ids")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> RouteStepIds { get; set; } = new();

    [BsonElement("revealed_characters")]
    public List<TeamRevealedCharacter> RevealedCharacters { get; set; } = new();

    [BsonElement("is_between_steps")]
    public bool IsBetweenSteps { get; set; }

    [BsonElement("unlocked_step_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? UnlockedStepId { get; set; }

    [BsonElement("final_word_attempt")]
    public string FinalWordAttempt { get; set; } = string.Empty;

    [BsonElement("final_word_accepted")]
    [BsonIgnoreIfNull]
    public bool? FinalWordAccepted { get; set; }

    [BsonElement("final_word_submitted_at")]
    [BsonIgnoreIfNull]
    public DateTime? FinalWordSubmittedAt { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("started_at")]
    [BsonIgnoreIfNull]
    public DateTime? StartedAt { get; set; }

    [BsonElement("finished_at")]
    [BsonIgnoreIfNull]
    public DateTime? FinishedAt { get; set; }
}
