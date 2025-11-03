using AF0E.DB;
using Logbook.Api.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Logbook.Api.Handlers;

public static class PotaGeoHandlers
{
    /// <summary>
    /// Returns points of activations. Can filter by state(s)
    /// </summary>
    public static async Task<GeoJsonData> GetActivationPoints(string states, HrdDbContext dbContext)
    {
        string[] st = [];
        if (states.Contains(','))
            st = states.Split(',');
        else if (states != "all") //single state or all
            st = [states];

        var actGrouped = await dbContext.PotaActivations
            .Where(x => st.Length == 0 || st.Contains(x.State))
            .Include(x => x.Park)
            .GroupBy(x => new Tuple<decimal, decimal>(x.Long, x.Lat))
            .ToListAsync();

        return new GeoJsonData
        {
            Type = "FeatureCollection",
            Features = GetActivatedParksInternal()
        };

        IEnumerable<object> GetActivatedParksInternal()
        {
            foreach (var ag in actGrouped)
                yield return new
                {
                    Type = "Feature",
                    Geometry = new
                    {
                        Type = "Point",
                        Coordinates = new[] { ag.Key.Item1, ag.Key.Item2 }, //Long, Lat
                    },
                    Properties = ag.Select(x => new
                    {
                        x.ActivationId,
                        x.StartDate,
                        x.Park.ParkNum,
                        x.Park.ParkName,
                    }).OrderByDescending(x => x.StartDate).ThenBy(x => x.ParkNum)
                };
        }
    }

    /// <summary>
    /// Returns activated parks with pota.app locations, not the activation locations like <see cref="GetActivationPoints"/>
    /// </summary>
    public static async Task<GeoJsonData> GetActivatedParks(HrdDbContext dbContext)
    {
        var parks = await dbContext.PotaParks
            .Where( p => dbContext.PotaActivations.Any(a => a.ParkId == p.ParkId))
            .Select(x => new
            {
                Type = "Feature",
                Geometry = new
                {
                    Type = "Point",
                    Coordinates = new[] { x.Long, x.Lat },
                },
                Properties = new
                {
                    x.ParkNum,
                    x.ParkName,
                    x.TotalActivationCount,
                    x.TotalQsoCount,
                }
            }).ToListAsync();

        return new GeoJsonData
        {
            Type = "FeatureCollection",
            Features = parks
        };
    }

    /// <summary>
    /// Returns locations for not yet activated parks by boundary
    /// </summary>
    public static async Task<GeoJsonData> GetNotActivatedParks(double swLat, double swLong, double neLat, double neLong, HrdDbContext dbContext)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var polygon = geometryFactory.CreatePolygon([
            new Coordinate(swLong, swLat),
            new Coordinate(neLong, swLat),
            new Coordinate(neLong, neLat),
            new Coordinate(swLong, neLat),
            new Coordinate(swLong, swLat)
        ]);

        var parks = await dbContext.PotaParks
            .Include(x => x.PotaActivations)
            .Where(x => x.Active && !x.PotaActivations.Any() && x.GeoPoint.Intersects(polygon))
            .OrderByDescending(x => x.TotalQsoCount) //so they, color circles, don't jump around on UI and least activated come on top
            .ThenBy(x => x.ParkId) // and newer parks on top of the others with the same qso count
            .Select(x => new
            {
                Type = "Feature",
                Geometry = new
                {
                    Type = "Point",
                    Coordinates = new[] { x.Long, x.Lat },
                },
                Properties = new
                {
                    x.ParkNum,
                    x.ParkName,
                    x.TotalActivationCount,
                    x.TotalQsoCount,
                }
            }).ToListAsync();

        return new GeoJsonData
        {
            Type = "FeatureCollection",
            Features = parks
        };
    }
}
