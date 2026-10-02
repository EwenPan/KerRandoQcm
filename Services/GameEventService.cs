namespace KerRandoQcm.Services;

/// <summary>
/// Service centralisé de notifications temps réel (SignalR / C# events en Blazor Server).
/// Diffuse instantanément les changements d'état à tous les écrans connectés sans rechargement.
/// </summary>
public class GameEventService
{
    public event Func<Task>? PlayersChanged;
    public event Func<Task>? TeamsChanged;
    public event Func<Task>? GameStateChanged;
    public event Func<string, Task>? TeamScoreChanged;
    public event Func<Task>? StationsChanged;
    public event Func<string, Task>? TeamProgressChanged;

    public async Task NotifyPlayersChangedAsync()
    {
        if (PlayersChanged != null)
        {
            var handlers = PlayersChanged.GetInvocationList().Cast<Func<Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(); } catch { /* ignore individual connection drops */ }
            }
        }
    }

    public async Task NotifyTeamsChangedAsync()
    {
        if (TeamsChanged != null)
        {
            var handlers = TeamsChanged.GetInvocationList().Cast<Func<Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(); } catch { /* ignore individual connection drops */ }
            }
        }
    }

    public async Task NotifyGameStateChangedAsync()
    {
        if (GameStateChanged != null)
        {
            var handlers = GameStateChanged.GetInvocationList().Cast<Func<Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(); } catch { /* ignore individual connection drops */ }
            }
        }
    }

    public async Task NotifyTeamScoreChangedAsync(string teamId)
    {
        if (TeamScoreChanged != null)
        {
            var handlers = TeamScoreChanged.GetInvocationList().Cast<Func<string, Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(teamId); } catch { /* ignore individual connection drops */ }
            }
        }
    }

    public async Task NotifyStationsChangedAsync()
    {
        if (StationsChanged != null)
        {
            var handlers = StationsChanged.GetInvocationList().Cast<Func<Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(); } catch { /* ignore individual connection drops */ }
            }
        }
    }

    public async Task NotifyTeamProgressChangedAsync(string teamId)
    {
        if (TeamProgressChanged != null)
        {
            var handlers = TeamProgressChanged.GetInvocationList().Cast<Func<string, Task>>();
            foreach (var handler in handlers)
            {
                try { await handler(teamId); } catch { /* ignore individual connection drops */ }
            }
        }
    }
}
