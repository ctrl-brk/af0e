using System.Net;
using AF0E.Common.Qrz;
using AF0E.DB;
using AF0E.Services.Pota;
using AF0E.Services.Qrz;
using Logbook.Api.Handlers;
using Logbook.Api.Models;
using Logbook.Api.Requests;
using Logbook.Api.Security;
using Microsoft.AspNetCore.Authorization;
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
        RegisterQrzEndpoints(builder);
        RegisterToolsEndpoints(builder);
    }

    private static void RegisterLogbookEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("logbook").WithTags("Logbook");

        builder.MapGet("lookup/{call}", async (string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall(WebUtility.UrlDecode(call), dbContext)))
            .WithName("LookupCall1");

        builder.MapGet("lookup/{prefix}/{call}", async (string prefix, string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}", dbContext)))
            .WithName("LookupCall2");

        builder.MapGet("lookup/{prefix}/{call}/{suffix}", async (string prefix, string call, string suffix, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLogByCall($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}/{WebUtility.UrlDecode(suffix)}", dbContext)))
            .WithName("LookupCall3");

        builder.MapGet("partial-lookup/{call}", async (string call, [FromQuery(Name = "max-results")] int? maxResults, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetPartialLookup(call, maxResults ?? 50, dbContext)))
            .WithName("LookupPartialCall");

        builder.MapGet("qso/{id:int}", async Task<Results<NotFound, Ok<QsoDetails>>> (int id, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext) =>
            {
                var res = await LogbookHandlers.GetQsoDetails(id, dbContext, authSvc, httpContext);
                return res is null ? TypedResults.NotFound() : TypedResults.Ok(res);
            })
            .WithName("QsoDetails");

        builder.MapPut("qso", async Task<Results<BadRequest<string>, NotFound, Ok<QsoDetails>>> (QsoDetails qso, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext) =>
            {
                try
                {
                    var res = await LogbookHandlers.UpdateQsoDetails(qso, dbContext, authSvc, httpContext);
                    return res is null ? TypedResults.NotFound() : TypedResults.Ok(res);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            })
            .RequireAuthorization(Policies.AdminOnly)
            .WithName("QsoUpdate");

        builder.MapPost("qso", async Task<Results<BadRequest<string>, Created<QsoDetails>>> (QsoRequest req, IQrzService qrzSvc, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext, CancellationToken ct) =>
            {
                try
                {
                    var result = await LogbookHandlers.CreateQso(req.Qso, dbContext, authSvc, httpContext);

                    if (req.PotaActivationId is not null)
                        await PotaHandlers.AddActivationQso(req.PotaActivationId.Value, result.Id, qrzSvc, dbContext, ct);

                    return TypedResults.Created($"/api/v1/logbook/qso/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(ex.Message);
                }
            })
            .RequireAuthorization(Policies.AdminOnly)
            .WithName("QsoCreate");

        builder.MapGet("{call?}", async (string? call, int? skip, int? take, string? sort, int? orderBy, string? begin, string? end, HrdDbContext dbContext) =>
                TypedResults.Ok(await LogbookHandlers.GetLog(call, skip, take, sort, orderBy, begin, end, dbContext)))
            .WithName("Logbook");
    }

    private static void RegisterPotaEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("pota").WithTags("Pota");

        builder.MapGet("activations", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetActivations(dbContext)))
            .WithName("PotaActivations");

        builder.MapGet("activations/{id:int}", async Task<Results<NotFound, Ok<PotaActivationDetails>>> (int id, HrdDbContext dbContext) =>
            {
                var res = await PotaHandlers.GetActivation(id, dbContext);
                return res is null ? TypedResults.NotFound() : TypedResults.Ok(res);
            })
            .WithName("PotaActivation");

        builder.MapGet("activations/{id:int}/log", async (int id, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetActivationLog(id, dbContext)))
            .WithName("PotaActivationLog");

        builder.MapPost("activations", async Task<Results<BadRequest<string>, Created<int>>> (NewActivationRequest req, HrdDbContext dbContext) =>
            {
                try
                {
                    var result = await PotaHandlers.CreateActivation(req, dbContext);
                    return TypedResults.Created($"/api/v1/pota/activations/{result}", result);
                }
                catch (ArgumentException e)
                {
                    return TypedResults.BadRequest(e.Message);
                }

            })
            .WithName("CreateActivation")
            .RequireAuthorization(Policies.AdminOnly);

        builder.MapPut("activations", async Task<Results<BadRequest<string>, NoContent>> (UpdateActivationRequest req, HrdDbContext dbContext) =>
            {
                try
                {
                    await PotaHandlers.UpdateActivation(req, dbContext);
                    return TypedResults.NoContent();
                }
                catch (ArgumentException e)
                {
                    return TypedResults.BadRequest(e.Message);
                }

            })
            .WithName("UpdateActivation")
            .RequireAuthorization(Policies.AdminOnly);

        builder.MapGet("parks/search/{parkNum}", async (string parkNum, [FromQuery(Name = "max-results")] int? maxResults, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetParks(parkNum, maxResults ?? 25, dbContext)))
            .WithName("ParkList");

        builder.MapGet("park/{parkNum}", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetPark(parkNum, dbContext)))
            .WithName("ParkDetails");

        builder.MapGet("park/{parkNum}/stats/hunting/log", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetParkHuntingLog(parkNum, dbContext)))
            .WithName("ParkHuntingLog");

        builder.MapGet("log/unconfirmed", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.GetUnconfirmedContacts(dbContext)))
            .WithName("PotaUnconfirmed");

        builder.MapGet("activity/{call}", async (string call, IPotaApiService potaApiService) =>
                TypedResults.Ok(await PotaHandlers.CheckActivity(WebUtility.UrlDecode(call), potaApiService)))
            .WithName("PotaActivityCall");

        builder.MapGet("activity", async (string? band, string? mode, string? dups, IPotaApiService potaApiService, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaHandlers.CheckActivity(band, mode, !string.IsNullOrEmpty(dups), potaApiService, dbContext)))
            .WithName("PotaActivity");


        builder = v1Builder.MapGroup("pota").WithTags("Pota GeoJson");

        // Returns points of activations. Can filter by state(s)
        builder.MapGet("geojson/activations/{states}", async (string states, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetActivationPoints(states, dbContext)))
            .WithName("GeoJsonActivations");

        // Returns activated parks with pota.app locations, not the activation locations like the method above
        builder.MapGet("geojson/parks/activated", async (HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetActivatedParks(dbContext)))
            .WithName("GeoJsonActivatedParks");

        // Returns locations for not yet activated parks by boundary
        builder.MapGet("geojson/parks/not-activated/boundary", async (double swLat, double swLong, double neLat, double neLong, HrdDbContext dbContext) =>
                TypedResults.Ok(await PotaGeoHandlers.GetNotActivatedParks(swLat, swLong, neLat, neLong, dbContext)))
            .WithName("GeoJsonSpatialParks");
    }

    private static void RegisterGridTrackerEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("gridtracker").WithTags("GridTracker");

        builder.MapGet("{call}", async (string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog(WebUtility.UrlDecode(call), dbContext)))
            .WithName("GridTrackerCallLookup1");

        builder.MapGet("{prefix}/{call}", async (string prefix, string call, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}", dbContext)))
            .WithName("GridtrackerCallLookup2");

        builder.MapGet("{prefix}/{call}/{suffix}", async (string prefix, string call, string suffix, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerLog($"{WebUtility.UrlDecode(prefix)}/{WebUtility.UrlDecode(call)}/{WebUtility.UrlDecode(suffix)}", dbContext)))
            .WithName("GridtrackerCallLookup3");

        builder.MapGet("pota/{parkNum}", async (string parkNum, HrdDbContext dbContext) =>
                TypedResults.Ok(await GridTrackerHandlers.GetGridTrackerParkStats(parkNum, dbContext)))
            .WithName("GridtrackerParkStats");
    }

    private static void RegisterQrzEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("qrz").WithTags("qrz.com");

        builder.MapGet("{call}", async Task<Results<NotFound, Ok<QrzCallsign>, ProblemHttpResult>> (string call, IQrzService qrzService, CancellationToken ct) =>
        {
            var (response, notFound) = await QrzHandlers.Lookup(call, qrzService, ct);

            if (notFound)
                return TypedResults.NotFound();

            return response is null ? TypedResults.Problem("QRZ lookup failed") : TypedResults.Ok(response.Callsign);
        })
        .RequireAuthorization(Policies.AdminOnly)
        .WithName("QrzCallLookup");
    }

    private static void RegisterToolsEndpoints(IEndpointRouteBuilder v1Builder)
    {
        var builder = v1Builder.MapGroup("tools").WithTags("tools");

        builder.MapGet("grid", Results<BadRequest<string>, Ok<string>> (string? latitude, string? longitude) =>
        {
            if (string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude))
                return TypedResults.BadRequest("Both latitude and longitude parameters are required");

            if (!double.TryParse(latitude, out var lat))
                return TypedResults.BadRequest($"Invalid latitude value: {latitude}");

            if (!double.TryParse(longitude, out var lon))
                return TypedResults.BadRequest($"Invalid longitude value: {longitude}");

            var square = UtilsHandlers.CoordinatesToGridSquare(lat, lon);

            if (square.Length != 6)
                return TypedResults.BadRequest(square);

            return TypedResults.Ok(square);
        })
        .WithName("GridLookup");
    }
}
