using KerRandoQcm.Models;
using KerRandoQcm.Services;
using Xunit;

namespace KerRandoQcm.Tests;

public class RouteGeneratorServiceTests
{
    [Fact]
    public void GenerateRoutes_CreatesDistinctRoutesWithoutRepeatedSteps()
    {
        var teams = CreateTeams(3);
        var steps = CreateSteps(4);
        var generator = new RouteGeneratorService();

        var routes = generator.GenerateRoutes(teams, steps);
        var routeKeys = routes.Values.Select(route => string.Join("/", route)).ToList();

        Assert.Equal(teams.Count, routes.Count);
        Assert.All(routes.Values, route => Assert.Equal(steps.Count, route.Count));
        Assert.Equal(routeKeys.Count, routeKeys.Distinct().Count());
        Assert.All(routes.Values, route => Assert.Equal(route.Count, route.Distinct().Count()));
    }

    [Fact]
    public void GenerateRoutes_DoesNotShareMoreThanTwoConsecutiveSteps()
    {
        var routes = new RouteGeneratorService().GenerateRoutes(CreateTeams(3), CreateSteps(5)).Values.ToList();

        for (var firstIndex = 0; firstIndex < routes.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < routes.Count; secondIndex++)
            {
                Assert.True(LongestSharedSegment(routes[firstIndex], routes[secondIndex]) <= 2);
            }
        }
    }

    [Fact]
    public void GenerateRoutes_SupportsEightTeamsAndEightSteps()
    {
        var teams = CreateTeams(8);
        var steps = CreateSteps(8);

        var routes = new RouteGeneratorService().GenerateRoutes(teams, steps);
        var routeValues = routes.Values.ToList();

        Assert.Equal(teams.Count, routes.Count);
        Assert.Equal(teams.Count, routeValues.Select(route => string.Join("/", route)).Distinct().Count());
        for (var firstIndex = 0; firstIndex < routeValues.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < routeValues.Count; secondIndex++)
            {
                Assert.True(LongestSharedSegment(routeValues[firstIndex], routeValues[secondIndex]) <= 2);
            }
        }
    }

    [Fact]
    public void GenerateRoutes_ThrowsWhenThereAreNotEnoughDistinctRoutes()
    {
        var teams = CreateTeams(3);
        var steps = CreateSteps(2);
        var generator = new RouteGeneratorService();

        Assert.Throws<InvalidOperationException>(() => generator.GenerateRoutes(teams, steps));
    }

    private static List<Team> CreateTeams(int count) => Enumerable.Range(1, count)
        .Select(index => new Team { Id = $"team-{index}" })
        .ToList();

    private static List<Step> CreateSteps(int count) => Enumerable.Range(1, count)
        .Select(index => new GameStep { Id = $"step-{index}" })
        .Cast<Step>()
        .ToList();

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
