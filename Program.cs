using System.Security.Claims;
using System.Text;
using KerRandoQcm.Components;
using KerRandoQcm.Data;
using KerRandoQcm.Data.InMemory;
using KerRandoQcm.Data.Mongo;
using KerRandoQcm.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddRazorPages();

var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("KerRandoQcm");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cookie-based admin authentication: once logged in with ADMIN_PASSWORD, the session persists
// across page navigations and browser restarts until the cookie expires or logout is triggered.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KerRandoAdminAuth";
        options.LoginPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Data store: in-memory by default (no external dependency), or MongoDB when "UseInMemoryStore" is false.
var useInMemoryStore = builder.Configuration.GetValue("UseInMemoryStore", true);
if (useInMemoryStore)
{
    builder.Services.AddSingleton<IGameDataStore, InMemoryGameDataStore>();
}
else
{
    builder.Services.AddSingleton<MongoService>();
    builder.Services.AddSingleton<IGameDataStore, MongoGameDataStore>();
}

builder.Services.AddSingleton<GameEventService>();
builder.Services.AddScoped<TeamBalancerService>();
builder.Services.AddScoped<RouteGeneratorService>();
builder.Services.AddSingleton<GamePlayService>();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddSingleton<ImageProcessingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/admin/authenticate", async (HttpContext ctx, IConfiguration config) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();
    var target = string.IsNullOrEmpty(returnUrl) ? "/admin" : returnUrl;

    var configuredPassword = config["ADMIN_PASSWORD"] ?? "admin123";
    if (password != configuredPassword)
    {
        return Results.Redirect($"/admin/login?error=1&returnUrl={Uri.EscapeDataString(target)}");
    }

    var identity = new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.Name, "admin") },
        CookieAuthenticationDefaults.AuthenticationScheme);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

    return Results.Redirect(target);
});

app.MapPost("/admin/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapGet("/admin/export.csv", async (IGameDataStore data) =>
{
    var teams = await data.Teams.GetAllAsync();
    var steps = await data.Steps.GetAllAsync();
    var submissions = await data.Submissions.GetAllAsync();
    var scores = await data.GameScores.GetAllAsync();
    var stepsById = steps.ToDictionary(step => step.Id);
    var rows = new List<string[]>
    {
        new[] { "Équipe", "Couleur", "Score total", "Étapes terminées", "Début", "Fin", "Parcours", "Réponses QCM", "Scores bruts des jeux", "Points de classement des jeux" }
    };

    foreach (var team in teams)
    {
        var route = team.RouteStepIds.Where(stepsById.ContainsKey).Select(stepId => stepsById[stepId]).ToList();
        var teamSubmissions = submissions.Where(submission => submission.TeamId == team.Id).Select(submission =>
        {
            var stepName = stepsById.GetValueOrDefault(submission.StepId ?? string.Empty)?.Name ?? "Question";
            return $"{stepName}: {submission.ChosenAnswer} ({(submission.IsCorrect ? "correcte" : "incorrecte")}, {submission.PointsEarned} points)";
        });
        var teamScores = scores.Where(score => score.TeamId == team.Id).ToList();
        var rawScores = teamScores.Select(score => $"{stepsById.GetValueOrDefault(score.StepId)?.Name ?? "Jeu"}: {score.RawScore}");
        var rankPoints = teamScores.Where(score => score.RankPointsAwarded.HasValue)
            .Select(score => $"{stepsById.GetValueOrDefault(score.StepId)?.Name ?? "Jeu"}: {score.RankPointsAwarded}");
        rows.Add(new[]
        {
            team.Name,
            team.Color ?? string.Empty,
            team.TotalScore.ToString(),
            Math.Clamp(team.CurrentStep - 1, 0, route.Count).ToString(),
            team.StartedAt?.ToString("O") ?? string.Empty,
            team.FinishedAt?.ToString("O") ?? string.Empty,
            string.Join(" > ", route.Select(step => step.Name)),
            string.Join(" | ", teamSubmissions),
            string.Join(" | ", rawScores),
            string.Join(" | ", rankPoints)
        });
    }

    var csv = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(EscapeCsv))));
    return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", "kerrando-resultats.csv");
}).RequireAuthorization();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
