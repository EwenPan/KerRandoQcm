using KerRandoQcm.Data;
using KerRandoQcm.Models;

namespace KerRandoQcm.Services;

public class TeamBalancerService
{
    private readonly IGameDataStore _data;

    public TeamBalancerService(IGameDataStore data)
    {
        _data = data;
    }

    private static readonly (string Name, string Color)[] DefaultTeamThemes = new[]
    {
        ("Équipe Rubis", "#e11d48"),
        ("Équipe Saphir", "#2563eb"),
        ("Équipe Émeraude", "#059669"),
        ("Équipe Ambre", "#d97706"),
        ("Équipe Améthyste", "#7c3aed"),
        ("Équipe Topaze", "#0891b2"),
        ("Équipe Corail", "#ea580c"),
        ("Équipe Jade", "#16a34a")
    };

    /// <summary>
    /// Répartit les joueurs inscrits dans N équipes en minimisant la co-présence d'amis d'un même groupe.
    /// </summary>
    public async Task<List<Team>> GenerateBalancedTeamsAsync(int numberOfTeams)
    {
        if (numberOfTeams < 2) numberOfTeams = 2;

        var players = await _data.Players.GetAllAsync();

        // 1. Initialiser les équipes en mémoire
        var teamSlots = new List<(Team team, List<Player> members)>();
        for (int i = 0; i < numberOfTeams; i++)
        {
            var theme = DefaultTeamThemes[i % DefaultTeamThemes.Length];
            var team = new Team
            {
                Name = theme.Name,
                Color = theme.Color,
                CurrentStep = 1,
                TotalScore = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            teamSlots.Add((team, new List<Player>()));
        }

        // 2. Regrouper les joueurs par groupe d'amis
        // Ceux sans groupe sont traités comme des groupes isolés de taille 1
        var groupedPlayers = players
            .GroupBy(p => string.IsNullOrWhiteSpace(p.FriendGroupId) ? Guid.NewGuid().ToString() : p.FriendGroupId)
            .OrderByDescending(g => g.Count()) // Les plus grands groupes d'abord
            .ToList();

        // 3. Distribution gloutonne équilibrée
        foreach (var group in groupedPlayers)
        {
            var groupList = group.ToList();
            foreach (var player in groupList)
            {
                // Trouver la meilleure équipe pour ce joueur :
                // Priorité 1 : équipe ayant le moins de membres du MÊME groupe d'amis
                // Priorité 2 : équipe ayant le moins de membres au total
                var bestSlot = teamSlots
                    .OrderBy(slot => slot.members.Count(m => !string.IsNullOrEmpty(m.FriendGroupId) && m.FriendGroupId == player.FriendGroupId))
                    .ThenBy(slot => slot.members.Count)
                    .First();

                bestSlot.members.Add(player);
            }
        }

        // 4. Enregistrer les équipes
        // Supprimer les équipes précédentes pour régénération propre
        await _data.Teams.DeleteAllAsync();

        var createdTeams = new List<Team>();
        foreach (var slot in teamSlots)
        {
            await _data.Teams.InsertOneAsync(slot.team);
            createdTeams.Add(slot.team);

            // Mettre à jour les joueurs avec leur TeamId
            foreach (var player in slot.members)
            {
                await _data.Players.UpdateOneAsync(p => p.Id == player.Id, p => p.TeamId = slot.team.Id);
            }
        }

        return createdTeams;
    }
}
