using AF0E.Common.Utils;

namespace Logbook.Api.Handlers;

public static class UtilsHandlers
{
    public static string CoordinatesToGridSquare(double lat, double lon)
    {
        return Utils.CoordinatesToGridSquare(lat, lon);
    }
}
