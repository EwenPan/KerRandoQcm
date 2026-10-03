using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using KerRandoQcm.Data;
using KerRandoQcm.Models;

namespace KerRandoQcm.Services;

public sealed record DeviceRegistrationResult(bool Allowed, string Token, bool IsNew);
public sealed record QcmSubmissionResult(bool Accepted, bool IsCorrect, int PointsEarned, string Message);
public sealed record GameScoreResult(bool Accepted, string Message);
public sealed record GameActionResult(bool Accepted, string Message);
public sealed record FinalWordResult(bool Submitted, bool IsCorrect, string Message);

public class GamePlayService
{
    private readonly IGameDataStore _data;
    private readonly SemaphoreSlim _submissionLock = new(1, 1);

    public GamePlayService(IGameDataStore data)
    {
        _data = data;
    }

    public async Task<DeviceRegistrationResult> RegisterDeviceAsync(string teamId, string? token)
    {
        var rawToken = string.IsNullOrWhiteSpace(token) ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : token;
        var tokenHash = HashToken(rawToken);
        var devices = await _data.TeamDevices.FindAsync(d => d.TeamId == teamId && d.IsActive);
        var currentDevice = devices.FirstOrDefault(d => d.TokenHash == tokenHash);

        if (currentDevice != null)
        {
            await _data.TeamDevices.UpdateOneAsync(d => d.Id == currentDevice.Id, d => d.LastSeenAt = DateTime.UtcNow);
            return new DeviceRegistrationResult(true, rawToken, false);
        }

        await _data.TeamDevices.InsertOneAsync(new TeamDevice
        {
            TeamId = teamId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });

        return new DeviceRegistrationResult(true, rawToken, true);
    }

    public async Task<bool> IsAuthorizedDeviceAsync(string teamId, string token)
    {
        var tokenHash = HashToken(token);
        return await _data.TeamDevices.FindOneAsync(d =>
            d.TeamId == teamId && d.TokenHash == tokenHash && d.IsActive) != null;
    }

    public async Task<List<Step>> GetTeamRouteAsync(Team team)
    {
        var steps = new List<Step>();
        foreach (var stepId in team.RouteStepIds)
        {
            var step = await _data.Steps.FindOneAsync(s => s.Id == stepId && s.IsActive);
            if (step != null)
            {
                steps.Add(step);
            }
        }
        return steps;
    }

    public async Task<QcmSubmissionResult> SubmitQcmAsync(string teamId, string token, Step step, Question question, Choice choice)
    {
        await _submissionLock.WaitAsync();
        try
        {
            if (!await IsGameInProgressAsync())
            {
                return new QcmSubmissionResult(false, false, 0, "Le rallye n'est pas en cours.");
            }

            var tokenHash = HashToken(token);
            var device = await _data.TeamDevices.FindOneAsync(d =>
                d.TeamId == teamId && d.TokenHash == tokenHash && d.IsActive);
            if (device == null)
            {
                return new QcmSubmissionResult(false, false, 0, "Cet appareil n'est pas autorisé pour cette équipe.");
            }

            var team = await _data.Teams.FindOneAsync(t => t.Id == teamId);
            if (team == null)
            {
                return new QcmSubmissionResult(false, false, 0, "Équipe introuvable.");
            }

            var route = await GetTeamRouteAsync(team);
            var currentIndex = team.CurrentStep - 1;
            if (team.IsBetweenSteps || currentIndex < 0 || currentIndex >= route.Count || route[currentIndex].Id != step.Id)
            {
                return new QcmSubmissionResult(false, false, 0, "Cette étape n'est pas l'étape attendue.");
            }
            if (team.UnlockedStepId != step.Id)
            {
                return new QcmSubmissionResult(false, false, 0, "Scannez le QR code de cette étape ou saisissez son code de secours.");
            }

            var existing = await _data.Submissions.FindOneAsync(s => s.TeamId == teamId && s.StepId == step.Id);
            if (existing != null)
            {
                return new QcmSubmissionResult(false, existing.IsCorrect, existing.PointsEarned, "Cette étape a déjà été validée.");
            }

            var savedQuestion = await _data.Questions.FindOneAsync(item => item.Id == question.Id);
            if (savedQuestion == null)
            {
                return new QcmSubmissionResult(false, false, 0, "Cette question n'est plus disponible.");
            }
            question = savedQuestion;
            var isCorrect = question.CorrectAnswer == choice;
            var points = isCorrect ? question.Points : 0;
            await _data.Submissions.InsertOneAsync(new Submission
            {
                TeamId = teamId,
                QuestionId = question.Id,
                StepId = step.Id,
                StationId = string.IsNullOrWhiteSpace(question.StationId) ? null : question.StationId,
                ChosenAnswer = choice,
                IsCorrect = isCorrect,
                PointsEarned = points,
                SubmittedAt = DateTime.UtcNow
            });

            await _data.Teams.UpdateOneAsync(t => t.Id == teamId, t =>
            {
                t.CurrentStep = currentIndex + 2;
                t.IsBetweenSteps = t.CurrentStep <= route.Count;
                t.UnlockedStepId = null;
                t.TotalScore += points;
                t.StartedAt ??= DateTime.UtcNow;
                if (t.CurrentStep > route.Count) t.FinishedAt = DateTime.UtcNow;
                t.UpdatedAt = DateTime.UtcNow;
            });
            await RevealProgressCharactersAsync(teamId, step, currentIndex, route.Count);
            await _data.TeamDevices.UpdateOneAsync(d => d.Id == device.Id, d => d.LastSeenAt = DateTime.UtcNow);

            return new QcmSubmissionResult(true, isCorrect, points, isCorrect ? "Bonne réponse !" : "Réponse enregistrée.");
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    public async Task<GameScoreResult> SubmitGameScoreAsync(string teamId, string token, GameStep step, int score)
    {
        await _submissionLock.WaitAsync();
        try
        {
            if (!await IsGameInProgressAsync())
            {
                return new GameScoreResult(false, "Le rallye n'est pas en cours.");
            }

            if (!await IsAuthorizedDeviceAsync(teamId, token))
            {
                return new GameScoreResult(false, "Cet appareil n'est pas autorisé pour cette équipe.");
            }

            var team = await _data.Teams.FindOneAsync(t => t.Id == teamId);
            if (team == null)
            {
                return new GameScoreResult(false, "Équipe introuvable.");
            }

            var route = await GetTeamRouteAsync(team);
            var currentIndex = team.CurrentStep - 1;
            if (team.IsBetweenSteps || currentIndex < 0 || currentIndex >= route.Count || route[currentIndex].Id != step.Id)
            {
                return new GameScoreResult(false, "Cette étape n'est pas l'étape Jeu attendue.");
            }
            if (team.UnlockedStepId != step.Id)
            {
                return new GameScoreResult(false, "Scannez le QR code de cette étape ou saisissez son code de secours.");
            }

            if (score < step.MinPoints || score > step.MaxPoints)
            {
                return new GameScoreResult(false, $"Le score doit être compris entre {step.MinPoints} et {step.MaxPoints}.");
            }

            var existing = await _data.GameScores.FindOneAsync(gameScore =>
                gameScore.TeamId == teamId && gameScore.StepId == step.Id);
            if (existing != null)
            {
                return new GameScoreResult(false, "Le score de cette étape a déjà été enregistré.");
            }

            var device = await _data.TeamDevices.FindOneAsync(deviceEntry =>
                deviceEntry.TeamId == teamId && deviceEntry.TokenHash == HashToken(token) && deviceEntry.IsActive);
            await _data.GameScores.InsertOneAsync(new GameScore
            {
                TeamId = teamId,
                StepId = step.Id,
                RawScore = score,
                DeviceId = device?.Id ?? string.Empty,
                SubmittedAt = DateTime.UtcNow
            });

            var competitionClosed = await CloseGameRankingIfCompleteAsync(step.Id);

            await _data.Teams.UpdateOneAsync(t => t.Id == teamId, t =>
            {
                t.CurrentStep = currentIndex + 2;
                t.IsBetweenSteps = t.CurrentStep <= route.Count;
                t.UnlockedStepId = null;
                t.StartedAt ??= DateTime.UtcNow;
                if (t.CurrentStep > route.Count) t.FinishedAt = DateTime.UtcNow;
                t.UpdatedAt = DateTime.UtcNow;
            });
            await RevealProgressCharactersAsync(teamId, step, currentIndex, route.Count);

            var resultMessage = competitionClosed
                ? $"Score enregistré : {score}. Le classement de ce jeu est définitif."
                : $"Score enregistré : {score}. Les points de classement seront attribués quand toutes les équipes auront terminé ce jeu.";
            return new GameScoreResult(true, resultMessage);
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    public async Task<List<string>> UpdateQuestionAsync(Question question)
    {
        if (question.Points < 0) throw new InvalidOperationException("Les points doivent être positifs ou nuls.");
        await _submissionLock.WaitAsync();
        try
        {
            if (await _data.Questions.FindOneAsync(item => item.Id == question.Id) == null)
                throw new InvalidOperationException("Cette question n'existe plus.");

            await _data.Questions.ReplaceOneAsync(question);
            var submissions = await _data.Submissions.FindAsync(item => item.QuestionId == question.Id);
            foreach (var submission in submissions)
            {
                await _data.Submissions.UpdateOneAsync(item => item.Id == submission.Id, item =>
                {
                    item.IsCorrect = item.ChosenAnswer == question.CorrectAnswer;
                    item.PointsEarned = item.IsCorrect ? question.Points : 0;
                });
            }

            var teamIds = submissions.Select(item => item.TeamId).Distinct().ToList();
            foreach (var teamId in teamIds) await RecalculateTeamScoreAsync(teamId);
            return teamIds;
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    public async Task<GameActionResult> CorrectQcmAnswerAsync(string submissionId, Choice choice)
    {
        await _submissionLock.WaitAsync();
        try
        {
            var submission = await _data.Submissions.FindOneAsync(item => item.Id == submissionId);
            if (submission == null) return new GameActionResult(false, "Cette réponse n'existe plus.");
            var question = await _data.Questions.FindOneAsync(item => item.Id == submission.QuestionId);
            if (question == null) return new GameActionResult(false, "Cette question n'existe plus.");
            if (!Enum.IsDefined(choice) || (question.Options.Count > 0 && !question.Options.Any(option => option.Choice == choice)))
                return new GameActionResult(false, "Choisissez une réponse disponible pour cette question.");

            await _data.Submissions.UpdateOneAsync(item => item.Id == submissionId, item =>
            {
                item.ChosenAnswer = choice;
                item.IsCorrect = choice == question.CorrectAnswer;
                item.PointsEarned = item.IsCorrect ? question.Points : 0;
            });
            await RecalculateTeamScoreAsync(submission.TeamId);
            return new GameActionResult(true, "Réponse modifiée et score mis à jour.");
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    public async Task<GameActionResult> CorrectGameScoreAsync(string scoreId, int rawScore)
    {
        await _submissionLock.WaitAsync();
        try
        {
            var score = await _data.GameScores.FindOneAsync(item => item.Id == scoreId);
            if (score == null) return new GameActionResult(false, "Ce score n'existe plus.");
            var step = await _data.Steps.FindOneAsync(item => item.Id == score.StepId) as GameStep;
            if (step == null) return new GameActionResult(false, "Cette étape de jeu n'existe plus.");
            if (rawScore < step.MinPoints || rawScore > step.MaxPoints)
                return new GameActionResult(false, $"Le score doit être compris entre {step.MinPoints} et {step.MaxPoints}.");

            await _data.GameScores.UpdateOneAsync(item => item.Id == scoreId, item => item.RawScore = rawScore);
            var rankingComplete = await CloseGameRankingIfCompleteAsync(step.Id);
            var scores = await _data.GameScores.FindAsync(item => item.StepId == step.Id);
            if (!rankingComplete)
            {
                foreach (var gameScore in scores)
                    await _data.GameScores.UpdateOneAsync(item => item.Id == gameScore.Id, item => item.RankPointsAwarded = null);
            }
            foreach (var teamId in scores.Select(item => item.TeamId).Distinct())
                await RecalculateTeamScoreAsync(teamId);

            return new GameActionResult(true, rankingComplete
                ? "Score modifié. Classement du jeu et totaux recalculés."
                : "Score modifié. Classement en attente des autres équipes.");
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    private async Task RecalculateTeamScoreAsync(string teamId)
    {
        var submissions = await _data.Submissions.FindAsync(item => item.TeamId == teamId);
        var gameScores = await _data.GameScores.FindAsync(item => item.TeamId == teamId);
        var totalScore = submissions.Sum(item => item.PointsEarned) + gameScores.Sum(item => item.RankPointsAwarded ?? 0);
        await _data.Teams.UpdateOneAsync(item => item.Id == teamId, item =>
        {
            item.TotalScore = totalScore;
            item.UpdatedAt = DateTime.UtcNow;
        });
    }

    public async Task ResetDevicesAsync(string teamId)
    {
        await _data.TeamDevices.UpdateAllAsync(device =>
        {
            if (device.TeamId == teamId)
            {
                device.IsActive = false;
            }
        });
    }

    public async Task<GameActionResult> UnlockStepAsync(string teamId, string stepId)
    {
        if (!await IsGameInProgressAsync())
        {
            return new GameActionResult(false, "Le rallye n'est pas en cours.");
        }

        var team = await _data.Teams.FindOneAsync(currentTeam => currentTeam.Id == teamId);
        if (team == null)
        {
            return new GameActionResult(false, "Équipe introuvable.");
        }
        var route = await GetTeamRouteAsync(team);
        var currentIndex = team.CurrentStep - 1;
        if (currentIndex < 0 || currentIndex >= route.Count || route[currentIndex].Id != stepId)
        {
            return new GameActionResult(false, "Ce n'est pas l'étape attendue pour votre équipe.");
        }

        await _data.Teams.UpdateOneAsync(currentTeam => currentTeam.Id == teamId, currentTeam =>
        {
            currentTeam.UnlockedStepId = stepId;
            currentTeam.IsBetweenSteps = false;
            currentTeam.UpdatedAt = DateTime.UtcNow;
        });
        return new GameActionResult(true, "Étape déverrouillée.");
    }

    public async Task<FinalWordResult> SubmitFinalWordAsync(string teamId, string token, string answer)
    {
        await _submissionLock.WaitAsync();
        try
        {
            if (!await IsGameInProgressAsync())
            {
                return new FinalWordResult(false, false, "Le rallye n'est pas en cours.");
            }
            if (!await IsAuthorizedDeviceAsync(teamId, token))
            {
                return new FinalWordResult(false, false, "Cet appareil n'est pas autorisé pour cette équipe.");
            }

            var team = await _data.Teams.FindOneAsync(currentTeam => currentTeam.Id == teamId);
            if (team == null)
            {
                return new FinalWordResult(false, false, "Équipe introuvable.");
            }
            var route = await GetTeamRouteAsync(team);
            if (route.Count == 0 || team.CurrentStep <= route.Count || team.IsBetweenSteps)
            {
                return new FinalWordResult(false, false, "Terminez toutes les étapes avant de proposer le mot.");
            }
            if (team.FinalWordAccepted == true)
            {
                return new FinalWordResult(false, true, "Votre équipe a déjà trouvé le mot final.");
            }

            var gameState = await _data.GameStates.FindOneAsync(_ => true);
            if (gameState == null || string.IsNullOrWhiteSpace(gameState.FinalWord))
            {
                return new FinalWordResult(false, false, "Le mot final n'a pas été configuré.");
            }
            var normalizedAnswer = NormalizeWord(answer);
            if (normalizedAnswer.Length == 0)
            {
                return new FinalWordResult(false, false, "Saisissez une proposition avant de valider.");
            }

            var isCorrect = normalizedAnswer == NormalizeWord(gameState.FinalWord);
            await _data.Teams.UpdateOneAsync(currentTeam => currentTeam.Id == teamId, currentTeam =>
            {
                currentTeam.FinalWordAttempt = answer.Trim().ToUpperInvariant();
                currentTeam.FinalWordAccepted = isCorrect;
                currentTeam.FinalWordSubmittedAt = DateTime.UtcNow;
            });

            return new FinalWordResult(true, isCorrect, isCorrect
                ? "Bravo, vous avez trouvé le mot final !"
                : "Ce n'est pas le bon mot. Regardez les lettres et réessayez.");
        }
        finally
        {
            _submissionLock.Release();
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private async Task<bool> IsGameInProgressAsync()
    {
        var state = await _data.GameStates.FindOneAsync(_ => true);
        return state == null || state.Phase == GamePhase.IN_PROGRESS;
    }

    private async Task<bool> CloseGameRankingIfCompleteAsync(string stepId)
    {
        var participatingTeams = await _data.Teams.FindAsync(team => team.RouteStepIds.Contains(stepId));
        var scores = await _data.GameScores.FindAsync(score => score.StepId == stepId);
        if (participatingTeams.Count == 0 || participatingTeams.Any(team => scores.All(score => score.TeamId != team.Id)))
        {
            return false;
        }

        var orderedScores = scores.OrderByDescending(score => score.RawScore).ToList();
        for (var index = 0; index < orderedScores.Count; index++)
        {
            var currentScore = orderedScores[index];
            var tieStartIndex = orderedScores.FindIndex(score => score.RawScore == currentScore.RawScore);
            var rankPoints = Math.Max(10 - tieStartIndex, 0);
            var pointsDifference = rankPoints - (currentScore.RankPointsAwarded ?? 0);
            await _data.GameScores.UpdateOneAsync(score => score.Id == currentScore.Id, score =>
            {
                score.RankPointsAwarded = rankPoints;
            });
            if (pointsDifference != 0)
            {
                await _data.Teams.UpdateOneAsync(team => team.Id == currentScore.TeamId, team =>
                {
                    team.TotalScore += pointsDifference;
                });
            }
        }

        return true;
    }

    private async Task RevealProgressCharactersAsync(string teamId, Step step, int completedIndex, int stepCount)
    {
        var gameState = await _data.GameStates.FindOneAsync(_ => true);
        var letters = gameState?.ShuffledLetters ?? string.Empty;
        if (letters.Length == 0 || stepCount <= 0)
        {
            return;
        }

        var startIndex = (completedIndex * letters.Length + stepCount - 1) / stepCount;
        var endIndex = ((completedIndex + 1) * letters.Length + stepCount - 1) / stepCount;
        if (startIndex == endIndex) return;

        await _data.Teams.UpdateOneAsync(team => team.Id == teamId, team =>
        {
            for (var index = startIndex; index < endIndex; index++)
            {
                var position = index + 1;
                var existing = team.RevealedCharacters.FirstOrDefault(item => item.Position == position);
                if (existing == null)
                {
                    team.RevealedCharacters.Add(new TeamRevealedCharacter
                    {
                        Position = position,
                        Character = letters[index].ToString(),
                        StepId = step.Id
                    });
                }
                else
                {
                    existing.Character = letters[index].ToString();
                    existing.StepId = step.Id;
                }
            }
        });
    }

    private static string NormalizeWord(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var characters = decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
                && char.IsLetterOrDigit(character))
            .ToArray();
        return new string(characters).ToUpperInvariant();
    }
}
