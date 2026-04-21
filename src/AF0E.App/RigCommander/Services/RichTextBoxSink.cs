using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace RigCommander.Services;

/// <summary>
/// A Serilog sink that appends log events to a RichTextBox.
/// The control is attached lazily after the form is created.
/// Thread-safe: marshals writes to the UI thread.
/// </summary>
public sealed class RichTextBoxSink : ILogEventSink
{
    private static readonly MessageTemplateTextFormatter _formatter = new("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

    private RichTextBox? _target;

    public void Attach(RichTextBox target)
    {
        _target = target;
    }

    public void Emit(LogEvent logEvent)
    {
        var box = _target;
        if (box is null || box.IsDisposed)
            return;

        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var text = writer.ToString();
        var color = LevelColor(logEvent.Level);

        if (box.IsHandleCreated)
            box.BeginInvoke(() => AppendColoredLine(box, text, color));
    }

    private static void AppendColoredLine(RichTextBox box, string text, Color color)
    {
        box.SuspendLayout();
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = color;
        box.AppendText(text);
        box.SelectionColor = box.ForeColor;
        box.ScrollToCaret();
        box.ResumeLayout();
    }

    private static Color LevelColor(LogEventLevel level) => level switch
    {
        LogEventLevel.Fatal or LogEventLevel.Error => Color.OrangeRed,
        LogEventLevel.Warning => Color.DarkOrange,
        _ => SystemColors.ControlText
    };
}
