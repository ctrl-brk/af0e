using System.Diagnostics.CodeAnalysis;

namespace AF0E.Common.Utils;

public static class AdifParser
{
    [SuppressMessage("Design", "CA1002:Do not expose generic lists")]
    public static List<AdifRecord> Parse(string content)
    {
        var records = new List<AdifRecord>();
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        while (index < content.Length)
        {
            var open = content.IndexOf('<', index);
            if (open < 0)
                break;

            var close = content.IndexOf('>', open + 1);
            if (close < 0)
                break;

            var descriptor = content[(open + 1)..close].Trim();
            index = close + 1;

            if (descriptor.Length == 0)
                continue;

            if (descriptor.Equals("EOH", StringComparison.OrdinalIgnoreCase))
            {
                current.Clear();
                continue;
            }

            if (descriptor.Equals("EOR", StringComparison.OrdinalIgnoreCase))
            {
                if (current.Count > 0)
                {
                    records.Add(new AdifRecord(new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase)));
                    current.Clear();
                }

                continue;
            }

            var parts = descriptor.Split(':', 3);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var fieldLength) || fieldLength < 0)
                continue;

            if (index + fieldLength > content.Length)
                fieldLength = content.Length - index;

            var value = content.Substring(index, fieldLength).Trim();
            index += fieldLength;

            if (!string.IsNullOrWhiteSpace(parts[0]))
                current[parts[0].Trim()] = value;
        }

        if (current.Count > 0)
            records.Add(new AdifRecord(new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase)));

        return records;
    }
}

public sealed record AdifRecord(IReadOnlyDictionary<string, string> Fields)
{
    public string? this[string fieldName] => Fields.TryGetValue(fieldName, out var value) ? value : null;
}
