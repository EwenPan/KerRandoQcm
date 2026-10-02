using KerRandoQcm.Models;

namespace KerRandoQcm.Services;

public class RouteGeneratorService
{
    public Dictionary<string, List<string>> GenerateRoutes(IReadOnlyList<Team> teams, IReadOnlyList<Step> steps)
    {
        if (steps.Count == 0 || teams.Count == 0)
        {
            return teams.ToDictionary(team => team.Id, _ => new List<string>());
        }

        var routeIds = steps.Select(step => step.Id).ToList();
        if (CountPermutationsCapped(routeIds.Count, teams.Count) < teams.Count)
        {
            throw new InvalidOperationException("Impossible de générer assez de parcours différents pour toutes les équipes.");
        }

        var selectedRoutes = new List<List<string>>();
        var maximumAttempts = Math.Max(10_000, teams.Count * 10_000);
        for (var attempt = 0; selectedRoutes.Count < teams.Count && attempt < maximumAttempts; attempt++)
        {
            var candidate = Shuffle(routeIds);
            if (selectedRoutes.Any(route => route.SequenceEqual(candidate)
                || LongestSharedSegment(route, candidate) > 2))
            {
                continue;
            }

            selectedRoutes.Add(candidate);
        }

        if (selectedRoutes.Count != teams.Count)
        {
            throw new InvalidOperationException("Impossible de créer des parcours suffisamment différents. Réduisez le nombre d'équipes ou ajoutez des étapes.");
        }

        return teams.Select((team, index) => new { team.Id, Route = selectedRoutes[index] })
            .ToDictionary(item => item.Id, item => item.Route);
    }

    private static int CountPermutationsCapped(int count, int cap)
    {
        var result = 1;
        for (var factor = 2; factor <= count; factor++)
        {
            if (result > cap / factor) return cap;
            result *= factor;
        }
        return result;
    }

    private static List<string> Shuffle(IReadOnlyList<string> values)
    {
        var shuffled = values.ToList();
        for (var index = shuffled.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
        }
        return shuffled;
    }

    private static int LongestSharedSegment(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var longest = 0;
        for (var firstStart = 0; firstStart < first.Count; firstStart++)
        {
            for (var secondStart = 0; secondStart < second.Count; secondStart++)
            {
                var length = 0;
                while (firstStart + length < first.Count
                    && secondStart + length < second.Count
                    && first[firstStart + length] == second[secondStart + length])
                {
                    length++;
                }
                longest = Math.Max(longest, length);
            }
        }
        return longest;
    }
}
