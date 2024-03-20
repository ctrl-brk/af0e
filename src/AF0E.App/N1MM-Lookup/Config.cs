namespace N1MMLookup;

public class Config
{
    public Point Location { get; init; }
    public Size Size { get; init; }

    public bool IsValid()
    {
        if (Size.Width < 250 || Size.Height < 130)
            return false;

        Point[] points =
        [
            new Point(Location.X, Location.Y),
            new Point(Location.X + Size.Width, Location.Y),
            new Point(Location.X, Location.Y + Size.Height),
            new Point(Location.X + Size.Width, Location.Y + Size.Height),
        ];

        return points.Select(point => Screen.AllScreens.Any(screen => screen.Bounds.Contains(point))).All(pointFits => pointFits);
    }
}
