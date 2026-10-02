using KerRandoQcm.Models;

namespace KerRandoQcm.Data.InMemory;

/// <summary>
/// IGameDataStore implementation that keeps all data in memory. Register this instead of
/// MongoGameDataStore to run the app locally without installing MongoDB (data is not persisted).
/// </summary>
public class InMemoryGameDataStore : IGameDataStore
{
    public IRepository<Player> Players { get; } = new InMemoryRepository<Player>();
    public IRepository<FriendGroup> FriendGroups { get; } = new InMemoryRepository<FriendGroup>();
    public IRepository<Team> Teams { get; } = new InMemoryRepository<Team>();
    public IRepository<Station> Stations { get; } = new InMemoryRepository<Station>();
    public IRepository<Step> Steps { get; } = new InMemoryRepository<Step>();
    public IRepository<TeamDevice> TeamDevices { get; } = new InMemoryRepository<TeamDevice>();
    public IRepository<Question> Questions { get; } = new InMemoryRepository<Question>();
    public IRepository<Submission> Submissions { get; } = new InMemoryRepository<Submission>();
    public IRepository<GameScore> GameScores { get; } = new InMemoryRepository<GameScore>();
    public IRepository<GameState> GameStates { get; } = new InMemoryRepository<GameState>();
}
