using RigCommander.Abstractions;

namespace RigCommander.Services;

/// <summary>
/// Appends script activity lines to a RichTextBox, keeping at most MaxLines entries.
/// The control is attached lazily after the form is created.
/// Set <see cref="MinimumLevel"/> to suppress low-priority entries (default: Information).
/// </summary>
public sealed class RichTextBoxActivityLog : IScriptActivityLog
{
    private const int MaxLines = 100;

    private RichTextBox? _box;

    public ActivityLogLevel MinimumLevel { get; set; } = ActivityLogLevel.Information;

    public void Attach(RichTextBox box)
    {
        _box = box;
    }

    public void Log(ActivityLogLevel level, string message)
    {
        if (level < MinimumLevel)
            return;

        var box = _box;
        if (box is null || box.IsDisposed)
            return;

        if (box.IsHandleCreated)
            box.BeginInvoke(() => AppendLineCore(box, message, level));
    }

    private static void AppendLineCore(RichTextBox box, string message, ActivityLogLevel level)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

        var color = level switch
        {
            ActivityLogLevel.Debug       => Color.LightGray,
            ActivityLogLevel.Warning     => Color.DarkOrange,
            ActivityLogLevel.Error       => Color.Crimson,
            _                            => box.ForeColor
        };

        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = color;
        box.AppendText(line);
        box.SelectionColor = box.ForeColor;

        // Remove oldest lines when over the cap
        if (box.Lines.Length > MaxLines + 1)
        {
            var firstCharToKeep = box.GetFirstCharIndexFromLine(box.Lines.Length - MaxLines);
            box.Select(0, firstCharToKeep);
            box.SelectedText = "";
        }

        box.SelectionStart = box.TextLength;
        box.ScrollToCaret();
    }
}
