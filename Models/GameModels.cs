using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KerRandoQcm.Models;

public enum StationType { VILLAGE, SALLE }
public enum StepType { QCM, GAME }
public enum MediaType { NONE, IMAGE, AUDIO, VIDEO }
public enum AnswerContentType { TEXT, AUDIO, IMAGE }
public enum Choice { A, B, C, D }
public enum GamePhase { REGISTRATION, TEAM_BUILDING, READY, IN_PROGRESS, PAUSED, FINISHED }

public class Station : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public StationType Type { get; set; }

    [BsonElement("qr_token")]
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("short_code")]
    [BsonIgnoreIfNull]
    public string ShortCode { get; set; } = string.Empty;

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
}

public class TeamRevealedCharacter
{
    [BsonElement("position")]
    public int Position { get; set; }

    [BsonElement("character")]
    public string Character { get; set; } = string.Empty;

    [BsonElement("step_id")]
    public string StepId { get; set; } = string.Empty;
}

[BsonKnownTypes(typeof(QcmStep), typeof(GameStep))]
public class Step : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public StepType Type { get; set; }

    [BsonElement("destination")]
    public string Destination { get; set; } = string.Empty;

    [BsonElement("instructions")]
    public string Instructions { get; set; } = string.Empty;

    [BsonElement("transition_hint")]
    public string TransitionHint { get; set; } = string.Empty;

    [BsonElement("qr_token")]
    [BsonIgnoreIfNull]
    public string? QrToken { get; set; }

    [BsonElement("short_code")]
    [BsonIgnoreIfNull]
    public string? ShortCode { get; set; }

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
}

public class QcmStep : Step
{
    [BsonElement("question_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? QuestionId { get; set; }
}

public class GameStep : Step
{
    [BsonElement("min_points")]
    public int MinPoints { get; set; }

    [BsonElement("max_points")]
    public int MaxPoints { get; set; }
}

public class GameScore : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("step_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StepId { get; set; } = string.Empty;

    [BsonElement("raw_score")]
    public int RawScore { get; set; }

    [BsonElement("rank_points_awarded")]
    [BsonIgnoreIfNull]
    public int? RankPointsAwarded { get; set; }

    [BsonElement("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [BsonElement("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class TeamDevice : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("token_hash")]
    public string TokenHash { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("last_seen_at")]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
}

public class Question : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? StationId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("answer_type")]
    [BsonRepresentation(BsonType.String)]
    public AnswerContentType AnswerType { get; set; } = AnswerContentType.TEXT;

    [BsonElement("options")]
    public List<AnswerOption> Options { get; set; } = new();

    [BsonElement("media_type")]
    [BsonRepresentation(BsonType.String)]
    public MediaType MediaType { get; set; } = MediaType.NONE;

    [BsonElement("media_url")]
    public string? MediaUrl { get; set; }

    [BsonElement("image_data")]
    [BsonIgnoreIfNull]
    public string? ImageData { get; set; }

    [BsonElement("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [BsonElement("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [BsonElement("option_c")]
    public string OptionC { get; set; } = string.Empty;

    [BsonElement("option_d")]
    public string OptionD { get; set; } = string.Empty;

    [BsonElement("correct_answer")]
    [BsonRepresentation(BsonType.String)]
    public Choice CorrectAnswer { get; set; }

    [BsonElement("points")]
    public int Points { get; set; } = 10;
}

public class AnswerOption
{
    [BsonElement("choice")]
    [BsonRepresentation(BsonType.String)]
    public Choice Choice { get; set; }

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;
}

public class Submission : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("team_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("player_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PlayerId { get; set; }

    [BsonElement("question_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string QuestionId { get; set; } = string.Empty;

    [BsonElement("step_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? StepId { get; set; }

    [BsonElement("station_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? StationId { get; set; }

    [BsonElement("chosen_answer")]
    [BsonRepresentation(BsonType.String)]
    public Choice ChosenAnswer { get; set; }

    [BsonElement("is_correct")]
    public bool IsCorrect { get; set; }

    [BsonElement("points_earned")]
    public int PointsEarned { get; set; }

    [BsonElement("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class GameState : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("phase")]
    [BsonRepresentation(BsonType.String)]
    public GamePhase Phase { get; set; } = GamePhase.REGISTRATION;

    [BsonElement("started_at")]
    public DateTime? StartedAt { get; set; }

    [BsonElement("ended_at")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("final_word")]
    public string FinalWord { get; set; } = string.Empty;

    [BsonElement("shuffled_letters")]
    public string ShuffledLetters { get; set; } = string.Empty;

    [BsonElement("transition_hint")]
    public string TransitionHint { get; set; } = string.Empty;

    [BsonElement("transition_image_data")]
    [BsonIgnoreIfNull]
    public string? TransitionImageData { get; set; }
}
