namespace AF0E.Common.Utils;

public static class Utils
{
    public static string CoordinatesToGridSquare(double lat, double lon)
    {
        if (double.IsNaN(lat) || double.IsNaN(lon) || double.IsInfinity(lat) || double.IsInfinity(lon))
            return "Invalid coordinates";

        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            return "Coordinates out of range";

        // clamp boundaries (important!)
        lat = Math.Min(89.999999, Math.Max(-90, lat));
        lon = Math.Min(179.999999, Math.Max(-180, lon));

        lon += 180;
        lat += 90;

        var fieldLon = (char)('A' + (int)(lon / 20));
        var fieldLat = (char)('A' + (int)(lat / 10));

        var squareLon = (char)('0' + (int)((lon % 20) / 2));
        var squareLat = (char)('0' + (int)((lat % 10)));

        var subLon = (char)('a' + (int)((lon % 2) * 12));
        var subLat = (char)('a' + (int)((lat % 1) * 24));

        return $"{fieldLon}{fieldLat}{squareLon}{squareLat}{subLon}{subLat}";
    }
}
