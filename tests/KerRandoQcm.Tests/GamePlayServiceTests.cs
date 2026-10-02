using KerRandoQcm.Data;
using KerRandoQcm.Data.InMemory;
using KerRandoQcm.Models;
using KerRandoQcm.Services;
using Xunit;

namespace KerRandoQcm.Tests;

public class GamePlayServiceTests
{
    [Fact]
    public async Task SubmitGameScore_AwardsPointsAfterAllTeamsFinish()
    {
        var (data, service, step, teams, tokens) = await CreateGameAsync(2);

        var firstResult = await service.SubmitGameScoreAsync(teams[0].Id, tokens[0], step, 8);
        var pendingScore = await data.GameScores.FindOneAsync(score => score.TeamId == teams[0].Id);

        Assert.True(firstResult.Accepted);
        Assert.Null(pendingScore?.RankPointsAwarded);
        Assert.Equal(0, (await data.Teams.FindOneAsync(team => team.Id == teams[0].Id))?.TotalScore);

        var finalResult = await service.SubmitGameScoreAsync(teams[1].Id, tokens[1], step, 6);
        var scores = await data.GameScores.GetAllAsync();
        var updatedTeams = await data.Teams.GetAllAsync();

        Assert.True(finalResult.Accepted);
        Assert.Equal(10, scores.Single(score => score.TeamId == teams[0].Id).RankPointsAwarded);
        Assert.Equal(9, scores.Single(score => score.TeamId == teams[1].Id).RankPointsAwarded);
        Assert.Equal(10, updatedTeams.Single(team => team.Id == teams[0].Id).TotalScore);
        Assert.Equal(9, updatedTeams.Single(team => team.Id == teams[1].Id).TotalScore);
    }

    [Fact]
    public async Task SubmitGameScore_TiedTeamsSharePointsForTheSamePlace()
    {
        var (data, service, step, teams, tokens) = await CreateGameAsync(3);

        await service.SubmitGameScoreAsync(teams[0].Id, tokens[0], step, 8);
        await service.SubmitGameScoreAsync(teams[1].Id, tokens[1], step, 8);
        await service.SubmitGameScoreAsync(teams[2].Id, tokens[2], step, 4);

        var scores = await data.GameScores.GetAllAsync();

        Assert.Equal(10, scores.Single(score => score.TeamId == teams[0].Id).RankPointsAwarded);
        Assert.Equal(10, scores.Single(score => score.TeamId == teams[1].Id).RankPointsAwarded);
        Assert.Equal(8, scores.Single(score => score.TeamId == teams[2].Id).RankPointsAwarded);
    }

    [Fact]
    public async Task SubmitGameScore_RejectsScoresOutsideConfiguredRange()
    {
        var (data, service, step, teams, tokens) = await CreateGameAsync(1);

        await data.Teams.UpdateOneAsync(team => team.Id == teams[0].Id, team => team.UnlockedStepId = null);
        var beforeScan = await service.SubmitGameScoreAsync(teams[0].Id, tokens[0], step, 8);
        Assert.False(beforeScan.Accepted);
        Assert.True((await service.UnlockStepAsync(teams[0].Id, step.Id)).Accepted);

        var result = await service.SubmitGameScoreAsync(teams[0].Id, tokens[0], step, step.MaxPoints + 1);

        Assert.False(result.Accepted);
        Assert.Empty(await data.GameScores.GetAllAsync());
        Assert.Equal(1, (await data.Teams.FindOneAsync(team => team.Id == teams[0].Id))?.CurrentStep);
    }

    [Fact]
    public async Task SubmitQcm_AcceptsAndStoresIncorrectAnswer()
    {
        var data = new InMemoryGameDataStore();
        var step = new QcmStep { Name = "Test question" };
        var question = new Question { CorrectAnswer = Choice.A, Points = 10 };
        await data.Steps.InsertOneAsync(step);
        var team = new Team { Name = "Test team", RouteStepIds = new List<string> { step.Id } };
        await data.Teams.InsertOneAsync(team);
        await data.Questions.InsertOneAsync(question);
        await data.GameStates.InsertOneAsync(new GameState { Phase = GamePhase.IN_PROGRESS });

        var service = new GamePlayService(data);
        var token = (await service.RegisterDeviceAsync(team.Id, null)).Token;
        var beforeScan = await service.SubmitQcmAsync(team.Id, token, step, question, Choice.B);
        Assert.False(beforeScan.Accepted);
        Assert.Empty(await data.Submissions.GetAllAsync());
        Assert.True((await service.UnlockStepAsync(team.Id, step.Id)).Accepted);

        var result = await service.SubmitQcmAsync(team.Id, token, step, question, Choice.B);
        var submission = await data.Submissions.FindOneAsync(item => item.TeamId == team.Id && item.StepId == step.Id);
        var updatedTeam = await data.Teams.FindOneAsync(item => item.Id == team.Id);

        Assert.True(result.Accepted);
        Assert.False(result.IsCorrect);
        Assert.Equal("Réponse enregistrée.", result.Message);
        Assert.NotNull(submission);
        Assert.False(submission.IsCorrect);
        Assert.Equal(0, submission.PointsEarned);
        Assert.Equal(2, updatedTeam?.CurrentStep);
    }

    [Fact]
    public async Task TeamsOnReversedRoutesReceiveTheSameLettersAtTheSameProgress()
    {
        var data = new InMemoryGameDataStore();
        var firstStep = new GameStep { Name = "First", MinPoints = 0, MaxPoints = 10 };
        var secondStep = new GameStep { Name = "Second", MinPoints = 0, MaxPoints = 10 };
        await data.Steps.InsertManyAsync(new Step[] { firstStep, secondStep });
        var firstTeam = new Team { Name = "First team", RouteStepIds = new List<string> { firstStep.Id, secondStep.Id }, UnlockedStepId = firstStep.Id };
        var secondTeam = new Team { Name = "Second team", RouteStepIds = new List<string> { secondStep.Id, firstStep.Id }, UnlockedStepId = secondStep.Id };
        await data.Teams.InsertManyAsync(new[] { firstTeam, secondTeam });
        await data.GameStates.InsertOneAsync(new GameState
        {
            Phase = GamePhase.IN_PROGRESS,
            FinalWord = "TEAM",
            ShuffledLetters = "MATE"
        });
        var service = new GamePlayService(data);
        var firstToken = (await service.RegisterDeviceAsync(firstTeam.Id, null)).Token;
        var secondToken = (await service.RegisterDeviceAsync(secondTeam.Id, null)).Token;

        await service.SubmitGameScoreAsync(firstTeam.Id, firstToken, firstStep, 4);
        await service.SubmitGameScoreAsync(secondTeam.Id, secondToken, secondStep, 3);

        var updatedFirstTeam = await data.Teams.FindOneAsync(team => team.Id == firstTeam.Id);
        var updatedSecondTeam = await data.Teams.FindOneAsync(team => team.Id == secondTeam.Id);
        var firstLetters = string.Concat(updatedFirstTeam!.RevealedCharacters.OrderBy(item => item.Position).Select(item => item.Character));
        var secondLetters = string.Concat(updatedSecondTeam!.RevealedCharacters.OrderBy(item => item.Position).Select(item => item.Character));

        Assert.Equal("MA", firstLetters);
        Assert.Equal(firstLetters, secondLetters);
        Assert.True(updatedFirstTeam.IsBetweenSteps);
        Assert.True(updatedSecondTeam.IsBetweenSteps);

        var bypassResult = await service.SubmitGameScoreAsync(firstTeam.Id, firstToken, secondStep, 6);
        Assert.False(bypassResult.Accepted);

        var continueResult = await service.UnlockStepAsync(firstTeam.Id, secondStep.Id);
        var continuedTeam = await data.Teams.FindOneAsync(team => team.Id == firstTeam.Id);
        Assert.True(continueResult.Accepted);
        Assert.False(continuedTeam?.IsBetweenSteps);
        Assert.Equal(secondStep.Id, continuedTeam?.UnlockedStepId);
    }

    [Fact]
    public async Task SubmitFinalWord_AcceptsDifferentCaseAndMissingAccents()
    {
        var (data, service, step, teams, tokens) = await CreateGameAsync(1);
        await data.GameStates.InsertOneAsync(new GameState
        {
            Phase = GamePhase.IN_PROGRESS,
            FinalWord = "ÉTÉ",
            ShuffledLetters = "TÉÉ"
        });

        await service.SubmitGameScoreAsync(teams[0].Id, tokens[0], step, 7);
        var result = await service.SubmitFinalWordAsync(teams[0].Id, tokens[0], "ete");
        var updatedTeam = await data.Teams.FindOneAsync(team => team.Id == teams[0].Id);

        Assert.True(result.Submitted);
        Assert.True(result.IsCorrect);
        Assert.True(updatedTeam?.FinalWordAccepted);
    }

    private static async Task<(IGameDataStore Data, GamePlayService Service, GameStep Step, List<Team> Teams, List<string> Tokens)> CreateGameAsync(int teamCount)
    {
        var data = new InMemoryGameDataStore();
        var step = new GameStep
        {
            Name = "Test game",
            MinPoints = 0,
            MaxPoints = 10
        };
        await data.Steps.InsertOneAsync(step);

        var teams = Enumerable.Range(0, teamCount)
            .Select(index => new Team { Name = $"Team {index + 1}", RouteStepIds = new List<string> { step.Id }, UnlockedStepId = step.Id })
            .ToList();
        await data.Teams.InsertManyAsync(teams);

        var service = new GamePlayService(data);
        var tokens = new List<string>();
        foreach (var team in teams)
        {
            var registration = await service.RegisterDeviceAsync(team.Id, null);
            tokens.Add(registration.Token);
        }

        return (data, service, step, teams, tokens);
    }
}