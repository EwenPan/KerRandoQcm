using KerRandoQcm.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace KerRandoQcm.Data;

public class MongoService
{
    private readonly IMongoDatabase _database;

    public MongoService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var databaseName = configuration.GetSection("MongoDbSettings")["DatabaseName"] ?? "KerRandoQcmDb";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Player> Players => _database.GetCollection<Player>("players");
    public IMongoCollection<FriendGroup> FriendGroups => _database.GetCollection<FriendGroup>("friend_groups");
    public IMongoCollection<Team> Teams => _database.GetCollection<Team>("teams");
    public IMongoCollection<Station> Stations => _database.GetCollection<Station>("stations");
    public IMongoCollection<Step> Steps => _database.GetCollection<Step>("steps");
    public IMongoCollection<TeamDevice> TeamDevices => _database.GetCollection<TeamDevice>("team_devices");
    public IMongoCollection<Question> Questions => _database.GetCollection<Question>("questions");
    public IMongoCollection<Submission> Submissions => _database.GetCollection<Submission>("submissions");
    public IMongoCollection<GameScore> GameScores => _database.GetCollection<GameScore>("game_scores");
    public IMongoCollection<GameState> GameStates => _database.GetCollection<GameState>("game_states");
}
