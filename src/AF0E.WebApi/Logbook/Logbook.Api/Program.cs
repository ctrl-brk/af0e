using System.Text.Json.Serialization;
using Log.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<HrdDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("HrdLog")));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGet("/api/v1/logbook/lookup/{call}", (string call, HrdDbContext dbContext) =>
    dbContext.Log.Where(x => x.ColCall == call).OrderByDescending(x => x.ColTimeOn)
)
.WithName("LookupCall")
.WithOpenApi();

app.MapGet("/api/v1/logbook/partial-lookup/{call}", async (string call, [FromQuery(Name = "max-results")] int? maxResults, HrdDbContext dbContext) =>
    {
        if (string.IsNullOrEmpty(call))
            return [];

        maxResults ??= 50;

        // select top({maxResults * 5}) is a nasty hack, but I can't figure out how to make this work any other way
        return await dbContext.Database.SqlQuery<string>($"""
                                                    select distinct top({maxResults}) COL_CALL
                                                      from (select top({maxResults * 5}) col_call
                                                              from TABLE_HRD_CONTACTS_V01
                                                             where COL_CALL like {call + "%"}
                                                             order by COL_TIME_ON desc) as a
                                                    """).ToListAsync();
    })
    .WithName("LookupPartialCall")
    .WithOpenApi();

// ReSharper disable once UnusedParameter.Local
app.MapGet("/api/v1/logbook/{call?}", async (string? call, int? skip, int? take, string? sort, int? orderBy, string? begin, string? end, HrdDbContext dbContext) =>
        {
            const int DEFAULT_PAGE_SIZE = 50;
            const int MAX_PAGE_SIZE = 500;

            skip ??= 0;
            if (take is null or > MAX_PAGE_SIZE) take = DEFAULT_PAGE_SIZE;

            var countQuery = begin is null || end is null ?
                dbContext.Log.CountAsync(x => call == null || x.ColCall == call) :
                dbContext.Log.CountAsync(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));

            var cnt = await countQuery;

            var logQuery = begin is null || end is null ?
                dbContext.Log.Where(x => call == null || x.ColCall == call) :
                dbContext.Log.Where(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));

            logQuery = logQuery.Include(c => c.PotaContacts);

            logQuery = orderBy == 1 ? logQuery.OrderBy(x => x.ColTimeOn) : logQuery.OrderByDescending(x => x.ColTimeOn);
            logQuery = logQuery.Skip(skip.Value).Take(take.Value);

            return logQuery
                .Select(x => new
                {
                    TotalCount = cnt,
                    Id = x.ColPrimaryKey,
                    Date = x.ColTimeOn,
                    Call = x.ColCall,
                    Band = x.ColBand,
                    Mode = x.ColMode,
                    SatName = x.ColSatName,
                    POTACount = x.PotaContacts.Count,
                });
        }
    )
    .WithName("Logbook")
    .WithOpenApi();

app.MapGet("/api/v1/logbook/qso/{id:int}", async (int id, HrdDbContext dbContext) =>
    {
        var res = await dbContext.Log
            .Include(x => x.PotaContacts)
            .ThenInclude(c => c.Activation)
            .ThenInclude(p => p.Park)
            .SingleOrDefaultAsync(x => x.ColPrimaryKey == id);
        return res == null
            ? Results.NotFound()
            : Results.Ok(new
            {
                Id = res.ColPrimaryKey,
                Date = res.ColTimeOn,
                Call = res.ColCall,
                Band = res.ColBand,
                BandRx = res.ColBandRx,
                Freq = res.ColFreq,
                FreqRx = res.ColFreqRx,
                Mode = res.ColMode,
                RstSent = res.ColRstSent,
                RstRcvd = res.ColRstRcvd,
                MyCity = res.ColMyCity,
                MyCounty = res.ColMyCnty,
                MyState = res.ColMyState,
                MyCountry = res.ColMyCountry,
                MyCqZone = res.ColMyCqZone,
                MyItuZone = res.ColMyItuZone,
                MyGrid = res.ColMyGridsquare,
                QslSent = res.ColQslSent,
                QslSentDate = res.ColQslsdate,
                QslSentVia = res.ColQslSentVia,
                QslRcvd = res.ColQslRcvd,
                QslRcvdDate = res.ColQslrdate,
                QslRcvdVia = res.ColQslRcvdVia,
                //POTA = res.PotaContacts.Count == 0 ? string.Empty : string.Join(',', res.PotaContacts.Select(x => x.Activation.Park.ParkNum)),
                POTA = res.PotaContacts.Select(x => x.Activation.Park.ParkNum),
                p2p = res.PotaContacts.Count > 0 && !string.IsNullOrEmpty(res.PotaContacts.First().P2P),
                SatName = res.ColSatName,
                SatMode = res.ColSatMode,
                Contest = res.ColContestId,
                Comment = res.ColUserDefined0
            }
        );
    })
    .WithName("QSO")
    .WithOpenApi();

app.MapGet("/api/v1/pota/activations", async (HrdDbContext dbContext) =>
        await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new
            {
                Id = x.ActivationId,
                x.StartDate,
                x.EndDate,
                x.Park.ParkNum,
                x.Park.ParkName,
                x.PotaContacts.Count,
                CwCount = x.PotaContacts.Count(c => c.Log.ColMode == "CW"),
                DigiCount = x.PotaContacts.Count(c => c.Log.ColMode == "FT8" || c.Log.ColMode == "MFSK"),
                PhoneCount = x.PotaContacts.Count(c => c.Log.ColMode == "SSB" || c.Log.ColMode == "LSB" || c.Log.ColMode == "USB"),
            }).ToListAsync()
    )
    .WithName("PotaActivations")
    .WithOpenApi();

app.MapGet("/api/v1/pota/activations/{id:int}", async (int id, HrdDbContext dbContext) =>
    {
        var res = await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .FirstOrDefaultAsync(x => x.ActivationId == id);

        return res == null
            ? Results.NotFound()
            : Results.Ok(new
            {
                Id = res.ActivationId,
                res.StartDate,
                res.EndDate,
                res.LogSubmittedDate,
                res.Park.ParkNum,
                res.Park.ParkName,
                res.SiteComments,
                res.City,
                res.County,
                res.State,
                res.Lat,
                res.Long,
                res.PotaContacts.Count,
                CwCount = res.PotaContacts.Count(c => c.Log.ColMode is "CW"),
                DigiCount = res.PotaContacts.Count(c => c.Log.ColMode is "FT8" or "MFSK"),
                PhoneCount = res.PotaContacts.Count(c => c.Log.ColMode is "SSB" or "LSB" or "USB"),
            });
    })
    .WithName("PotaActivation")
    .WithOpenApi();

app.MapGet("/api/v1/pota/activations/{id:int}/log", async (int id, HrdDbContext dbContext) =>
        await dbContext.PotaContacts
            .Where(x => x.ActivationId == id)
            .Include(x => x.Log)
            .Select(x => new
            {
                LogId = x.Log.ColPrimaryKey,
                Date = x.Log.ColTimeOn,
                Call = x.Log.ColCall,
                Band = x.Log.ColBand,
                Mode = x.Log.ColMode,
                SatName = x.Log.ColSatName,
                p2p = x.P2P,
            }).ToListAsync()
    )
    .WithName("PotaActivationLog")
    .WithOpenApi();

app.MapGet("/api/v1/logbook/gridtracker/{call}", (string call, HrdDbContext dbContext) =>
        dbContext.Log
            .Where(x => x.ColCall == call)
            .OrderByDescending(x => x.ColTimeOn)
            .Select(x => new
            {
                x.ColTimeOn,
                x.ColMode,
                x.ColBand,
                x.ColComment
            })
    )
    .WithName("GridtrackerLookup")
    .WithOpenApi();

//---------------

app.MapFallbackToFile("/index.html");
app.Run();
