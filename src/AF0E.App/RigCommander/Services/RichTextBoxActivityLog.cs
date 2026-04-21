using RigCommander.Abstractions;

namespace RigCommander.Services;

/// <summary>
/// Appends script activity lines to a RichTextBox, keeping at most MaxLines entries.
/// The control is attached lazily after the form is created.
/// </summary>
public sealed class RichTextBoxActivityLog : IScriptActivityLog
{
    private const int MaxLines = 100;

    private RichTextBox? _box;

    public void Attach(RichTextBox box)
    {
        _box = box;
    }

    public void AppendLine(string message)
    {
        var box = _box;
        if (box is null || box.IsDisposed)
            return;

        if (box.IsHandleCreated)
            box.BeginInvoke(() => AppendLineCore(box, message));
    }

    private static void AppendLineCore(RichTextBox box, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        box.AppendText(line);

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
