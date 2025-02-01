namespace QslLabel.Forms;

internal static class ControlExtensions
{
    public static void SetDoubleBuffered(this Control control)
    {
        var propertyInfo = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        propertyInfo?.SetValue(control, true, null);
    }
}
