using System.Data;
using System.Data.OleDb;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<ScoreboardService>();
builder.Services.AddHostedService<ScoreboardPoller>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<ScoreHub>("/hub");

// Optional: raw JSON endpoint for debugging
app.MapGet("/api/snapshot", async (ScoreboardService svc) => Results.Json(await svc.GetSnapshotAsync()));

app.Run($"http://0.0.0.0:{builder.Configuration.GetValue<int>("Scoreboard:Port")}");


// ----------------------------

sealed class ScoreHub : Hub
{
    private readonly ScoreboardService _svc;
    public ScoreHub(ScoreboardService svc) => _svc = svc;

    public override async Task OnConnectedAsync()
    {
        // Send one snapshot immediately so the page isn't blank
        var snap = await _svc.GetSnapshotAsync();
        await Clients.Caller.SendAsync("snapshot", snap);
        await base.OnConnectedAsync();
    }
}

sealed class ScoreboardPoller : BackgroundService
{
    private readonly ScoreboardService _svc;
    private readonly IHubContext<ScoreHub> _hub;
    private readonly IConfiguration _cfg;

    public ScoreboardPoller(ScoreboardService svc, IHubContext<ScoreHub> hub, IConfiguration cfg)
    {
        _svc = svc; _hub = hub; _cfg = cfg;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshSeconds = _cfg.GetValue<int>("Scoreboard:RefreshSeconds", 10);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snap = await _svc.GetSnapshotAsync();
                await _hub.Clients.All.SendAsync("snapshot", snap, stoppingToken);
            }
            catch
            {
                // swallow + keep going (LAN/events > perfection)
            }

            await Task.Delay(TimeSpan.FromSeconds(refreshSeconds), stoppingToken);
        }
    }
}

sealed class ScoreboardService
{
    private readonly IConfiguration _cfg;
    public ScoreboardService(IConfiguration cfg) => _cfg = cfg;

    private OleDbConnection Open()
    {
        var path = _cfg.GetValue<string>("Scoreboard:MdbPath") ?? throw new Exception("Scoreboard:MdbPath missing");

        // For .mdb: Jet provider is classic, but most modern installs use ACE.
        // Use ACE first; it can open .mdb too.
        var cs = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Persist Security Info=False;Mode=Read;";
        var conn = new OleDbConnection(cs);
        conn.Open();
        return conn;
    }

    public async Task<object> GetSnapshotAsync()
    {
        using var conn = Open();

        // Helper local function
        async Task<DataTable> QueryAsync(string sql)
        {
            using var cmd = new OleDbCommand(sql, conn);
            using var da = new OleDbDataAdapter(cmd);
            var dt = new DataTable();
            await Task.Run(() => da.Fill(dt));
            return dt;
        }

        // PowerBI-equivalent points expression from fldModeContest:
        // PH=1, CW=2, DIG=2, else 0
        // (Handles nulls/whitespace and case)
        var pointsExpr =
            "IIf(UCase(Trim(IIf(IsNull(fldModeContest),'',fldModeContest)))='PH',1," +
            "IIf(UCase(Trim(IIf(IsNull(fldModeContest),'',fldModeContest)))='CW',2," +
            "IIf(UCase(Trim(IIf(IsNull(fldModeContest),'',fldModeContest)))='DIG',2,0)))";

        // Core aggregates from tblContacts
        var totalContacts = await QueryAsync("SELECT Count(*) AS TotalContacts FROM tblContacts;");
        var totalPoints = await QueryAsync($@"
            SELECT IIf(IsNull(Sum({pointsExpr})),0,Sum({pointsExpr})) AS TotalPoints
            FROM tblContacts;");

        var byOperatorContacts = await QueryAsync(@"
            SELECT fldOperator AS Operator, Count(*) AS Contacts
            FROM tblContacts
            GROUP BY fldOperator
            ORDER BY Count(*) DESC;");

        var byOperatorPoints = await QueryAsync($@"
            SELECT fldOperator AS Operator,
                   IIf(IsNull(Sum({pointsExpr})),0,Sum({pointsExpr})) AS Points
            FROM tblContacts
            GROUP BY fldOperator
            ORDER BY IIf(IsNull(Sum({pointsExpr})),0,Sum({pointsExpr})) DESC;");

        var byMode = await QueryAsync(@"
            SELECT fldMode AS Mode, Count(*) AS Contacts
            FROM tblContacts
            GROUP BY fldMode
            ORDER BY Count(*) DESC;");

        var byContinent = await QueryAsync(@"
            SELECT fldContinent AS Continent, Count(*) AS Contacts
            FROM tblContacts
            WHERE fldContinent Is Not Null AND fldContinent <> ''
            GROUP BY fldContinent
            ORDER BY Count(*) DESC;");

        var byState = await QueryAsync(@"
            SELECT fldState AS State, Count(*) AS Contacts
            FROM tblContacts
            WHERE fldState Is Not Null AND fldState <> ''
            GROUP BY fldState
            ORDER BY Count(*) DESC;");

        var byCountry = await QueryAsync(@"
            SELECT fldCountryWorked AS Country, Count(*) AS Contacts
            FROM tblContacts
            WHERE fldCountryWorked Is Not Null AND fldCountryWorked <> ''
            GROUP BY fldCountryWorked
            ORDER BY Count(*) DESC;");

        object TableToRows(DataTable dt) =>
            dt.Rows.Cast<DataRow>().Select(r =>
                dt.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c])
            ).ToList();

        return new
        {
            meta = new { generatedUtc = DateTime.UtcNow },
            totals = new
            {
                contacts = Convert.ToInt32(totalContacts.Rows[0]["TotalContacts"]),
                points = Convert.ToInt32(totalPoints.Rows[0]["TotalPoints"])
            },
            contactsByOperator = TableToRows(byOperatorContacts),
            pointsByOperator = TableToRows(byOperatorPoints),
            contactsByMode = TableToRows(byMode),
            contactsByContinent = TableToRows(byContinent),
            contactsByState = TableToRows(byState),
            contactsByCountry = TableToRows(byCountry)
        };
    }
}
