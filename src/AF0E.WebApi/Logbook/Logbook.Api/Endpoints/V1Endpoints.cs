using System.Net;
using AF0E.DB;
using Logbook.Api.Handlers;
using Logbook.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Logbook.Api.Endpoints;

public static class V1Endpoints
{
    public static void RegisterV1Endpoints(this WebApplication app)
    {
        var builder = app.MapGroup("api/v1");

        RegisterLogbookEndpoints(builder);
        RegisterPotaEndpoints(builder);
        RegisterGridTrackerEndpoints(builder);
    }

    private static void RegisterLogbookEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("logbook").WithTags("Logbook");

        builder.MapGet("lookup/{call}", async (string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall(WebUtility.UrlDecode(call), dbContext)))
            .WithName("LookupCall1")
            .WithOpenApi();

        builder.MapGet("lookup/{prefix}/{call}", async (string prefix, string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}", dbContext)))
            .WithName("LookupCall2")
            .WithOpenApi();

        builder.MapGet("lookup/{prefix}/{call}/{suffix}", async (string prefix, string call, string suffix, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}/{WebUtility.UrlDecode(suffix)}", dbContext)))
            .WithName("LookupCall3")
            .WithOpenApi();

        builder.MapGet("partial-lookup/{call}", async (string call, [FromQuery(Name = "max-results")] int? maxResults, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetPartialLookup(call, maxResults ?? 50, dbContext)))
            .WithName("LookupPartialCall")
            .WithOpenApi();

        builder.MapGet("qso/{id:int}", async Task<Results<NotFound, Ok<QsoDetails>>> (int id, HrdDbContext dbContext) =>
            {
                var res = await LogbookHandlers.GetQsoDetails(id, dbContext);
                return res is null ? TypedResults.NotFound() : TypedResults.Ok(res);
            })
            .WithName("QsoDetails")
            .WithOpenApi();

        builder.MapGet("{call?}", async (string? call, int? skip, int? take, string? sort, int? orderBy, string? begin, string? end, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLog(call, skip, take, sort, orderBy, begin, end, dbContext)))
            .WithName("Logbook")
            .WithOpenApi();
    }

    private static void RegisterPotaEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("pota").WithTags("Pota");

        builder.MapGet("activations", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetActivations(dbContext)))
            .WithName("PotaActivations")
            .WithOpenApi();

        builder.MapGet("activations/{id:int}", async Task<Results<NotFound, Ok<PotaActivationDetails>>> (int id, HrdDbContext dbContext) =>
            {
                var res = await PotaHandlers.GetActivation(id, dbContext);
                return res is null ? TypedResults.NotFound() : TypedResults.Ok(res);
            })
            .WithName("PotaActivation")
            .WithOpenApi();

        builder.MapGet("activations/{id:int}/log", async (int id, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetActivationLog(id, dbContext)))
            .WithName("PotaActivationLog")
            .WithOpenApi();

        builder.MapGet("parks/search/{parkNum}", async (string parkNum, [FromQuery(Name = "max-results")] int? maxResults, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetParks(parkNum, maxResults ?? 25, dbContext)))
            .WithName("ParkList")
            .WithOpenApi();

        builder.MapGet("park/{parkNum}", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetPark(parkNum, dbContext)))
            .WithName("ParkDetails")
            .WithOpenApi();

        builder.MapGet("park/{parkNum}/stats/hunting/log", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetParkHuntingLog(parkNum, dbContext)))
            .WithName("ParkHuntingLog")
            .WithOpenApi();

        builder.MapGet("log/unconfirmed", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetUnconfirmedContacts(dbContext)))
            .WithName("PotaUnconfirmed")
            .WithOpenApi();

        builder = v1Builder.MapGroup("pota").WithTags("Pota GeoJson");

        // Returns points of activations. Can filter by state(s)
        builder.MapGet("geojson/activations/{states}", async (string states, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetActivationPoints(states, dbContext)))
            .WithName("GeoJsonActivations")
            .WithOpenApi();

        // Returns activated parks with pota.app locations, not the activation locations like the method above
        builder.MapGet("geojson/parks/activated", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetActivatedParks(dbContext)))
            .WithName("GeoJsonActivatedParks")
            .WithOpenApi();

        // Returns locations for not yet activated parks by boundary
        builder.MapGet("geojson/parks/not-activated/boundary", async (double swLat, double swLong, double neLat, double neLong, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetNotActivatedParks(swLat, swLong, neLat, neLong, dbContext)))
            .WithName("GeoJsonSpatialParks")
            .WithOpenApi();
    }

    private static void RegisterGridTrackerEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("gridtracker").WithTags("GridTracker");

        builder.MapGet("{call}", async (string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog(WebUtility.UrlDecode(call), dbContext)))
            .WithName("GridTrackerCallLookup1")
            //.WithGroupName("GridTracker")
            .WithOpenApi();

        builder.MapGet("{prefix}/{call}", async (string prefix, string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}", dbContext)))
            .WithName("GridtrackerCallLookup2")
            .WithOpenApi();

        builder.MapGet("{prefix}/{call}/{suffix}", async (string prefix, string call, string suffix, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}/{WebUtility.UrlDecode(suffix)}", dbContext)))
            .WithName("GridtrackerCallLookup3")
            .WithOpenApi();

        builder.MapGet("pota/{parkNum}", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerParkStats(parkNum, dbContext)))
            .WithName("GridtrackerParkStats")
            .WithOpenApi();
    }
}
