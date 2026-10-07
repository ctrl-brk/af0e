using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace QslDueWatcher;

public interface IReminderStateStore
{
    Task<HashSet<string>> LoadAsync(CancellationToken cancellationToken);
    Task MarkSentAsync(IEnumerable<string> reminderKeys, CancellationToken cancellationToken);
}

public sealed class ReminderStateStore(IOptions<AppSettings> options) : IReminderStateStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _path = Path.GetFullPath(options.Value.StateFile, AppContext.BaseDirectory);

    public async Task<HashSet<string>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return [];

        await using var stream = File.OpenRead(_path);
        var state = await JsonSerializer.DeserializeAsync<ReminderState>(stream, _jsonOptions, cancellationToken);
        return state?.SentReminderKeys.ToHashSet(StringComparer.Ordinal) ?? [];
    }

    public async Task MarkSentAsync(IEnumerable<string> reminderKeys, CancellationToken cancellationToken)
    {
        var keys = await LoadAsync(cancellationToken);
        keys.UnionWith(reminderKeys);

        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);

        var tempPath = _path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, new ReminderState([.. keys.Order()]), _jsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, _path, true);
    }

    public static string CreateKey(QslDueReminder reminder, string recipient)
    {
        var identity = string.Create(CultureInfo.InvariantCulture,
            $"{reminder.EventId}|{reminder.DueDate:O}|{recipient.Trim().ToUpperInvariant()}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private sealed record ReminderState(IReadOnlyList<string> SentReminderKeys);
}
