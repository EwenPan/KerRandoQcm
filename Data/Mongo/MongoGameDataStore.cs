using KerRandoQcm.Models;

namespace KerRandoQcm.Data.Mongo;

/// <summary>IGameDataStore implementation backed by MongoDB, used when a real MongoDB server is available.</summary>
public class MongoGameDataStore : IGameDataStore
{
    public IRepository<Player> Players { get; }
    public IRepository<FriendGroup> FriendGroups { get; }
    public IRepository<Team> Teams { get; }
    public IRepository<Station> Stations { get; }
    public IRepository<Step> Steps { get; }
    public IRepository<TeamDevice> TeamDevices { get; }
    public IRepository<Question> Questions { get; }
    public IRepository<Submission> Submissions { get; }
    public IRepository<GameScore> GameScores { get; }
    public IRepository<GameState> GameStates { get; }

    public MongoGameDataStore(MongoService mongo)
    {
        Players = new MongoRepository<Player>(mongo.Players);
        FriendGroups = new MongoRepository<FriendGroup>(mongo.FriendGroups);
        Teams = new MongoRepository<Team>(mongo.Teams);
        Stations = new MongoRepository<Station>(mongo.Stations);
        Steps = new MongoRepository<Step>(mongo.Steps);
        TeamDevices = new MongoRepository<TeamDevice>(mongo.TeamDevices);
        Questions = new MongoRepository<Question>(mongo.Questions);
        Submissions = new MongoRepository<Submission>(mongo.Submissions);
        GameScores = new MongoRepository<GameScore>(mongo.GameScores);
        GameStates = new MongoRepository<GameState>(mongo.GameStates);
    }
}
