using KerRandoQcm.Models;

namespace KerRandoQcm.Data;

/// <summary>
/// Central access point to all game collections. Implemented either by MongoDB or by an in-memory
/// store, allowing the app to run locally without any database installed.
/// </summary>
public interface IGameDataStore
{
    IRepository<Player> Players { get; }
    IRepository<FriendGroup> FriendGroups { get; }
    IRepository<Team> Teams { get; }
    IRepository<Station> Stations { get; }
    IRepository<Step> Steps { get; }
    IRepository<TeamDevice> TeamDevices { get; }
    IRepository<Question> Questions { get; }
    IRepository<Submission> Submissions { get; }
    IRepository<GameScore> GameScores { get; }
    IRepository<GameState> GameStates { get; }
}
