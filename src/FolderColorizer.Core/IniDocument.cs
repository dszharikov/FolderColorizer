using System.Text;

namespace FolderColorizer.Core;

/// <summary>
/// A small, loss-conscious INI editor. Unrelated sections, comments and ordering
/// are retained while Folder Colorizer updates its own values.
/// </summary>
public sealed class IniDocument
{
    private readonly List<string> _lines;

    private IniDocument(IEnumerable<string> lines)
    {
        _lines = [.. lines];
    }

    public static IniDocument Empty() => new([]);

    public static IniDocument Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        string[] lines = normalized.Split('\n');

        if (lines.Length > 0 && lines[^1].Length == 0)
        {
            lines = lines[..^1];
        }

        return new IniDocument(lines);
    }

    public string? GetValue(string section, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        SectionRange? range = FindSection(section);
        if (range is null)
        {
            return null;
        }

        for (int index = range.Value.Start + 1; index < range.Value.End; index++)
        {
            if (TryParseKey(_lines[index], out string? candidate, out string? value) &&
                candidate.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    public void SetValue(string section, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        SectionRange? range = FindSection(section);
        if (range is null)
        {
            if (_lines.Count > 0 && _lines[^1].Length > 0)
            {
                _lines.Add(string.Empty);
            }

            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
            return;
        }

        for (int index = range.Value.Start + 1; index < range.Value.End; index++)
        {
            if (TryParseKey(_lines[index], out string? candidate, out _) &&
                candidate.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _lines[index] = $"{key}={value}";
                return;
            }
        }

        _lines.Insert(range.Value.End, $"{key}={value}");
    }

    public void RemoveValue(string section, string key)
    {
        SectionRange? range = FindSection(section);
        if (range is null)
        {
            return;
        }

        for (int index = range.Value.End - 1; index > range.Value.Start; index--)
        {
            if (TryParseKey(_lines[index], out string? candidate, out _) &&
                candidate.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _lines.RemoveAt(index);
            }
        }
    }

    public void RemoveSection(string section)
    {
        SectionRange? range = FindSection(section);
        if (range is null)
        {
            return;
        }

        int count = range.Value.End - range.Value.Start;
        _lines.RemoveRange(range.Value.Start, count);

        while (_lines.Count > 0 && _lines[^1].Length == 0)
        {
            _lines.RemoveAt(_lines.Count - 1);
        }
    }

    public bool HasValues() => _lines.Any(line => TryParseKey(line, out _, out _));

    public override string ToString() =>
        _lines.Count == 0 ? string.Empty : string.Join("\r\n", _lines) + "\r\n";

    private SectionRange? FindSection(string section)
    {
        for (int index = 0; index < _lines.Count; index++)
        {
            if (!TryParseSection(_lines[index], out string? candidate) ||
                !candidate.Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int end = index + 1;
            while (end < _lines.Count && !TryParseSection(_lines[end], out _))
            {
                end++;
            }

            return new SectionRange(index, end);
        }

        return null;
    }

    private static bool TryParseSection(string line, out string section)
    {
        string trimmed = line.Trim();
        if (trimmed.Length >= 3 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            section = trimmed[1..^1].Trim();
            return section.Length > 0;
        }

        section = string.Empty;
        return false;
    }

    private static bool TryParseKey(string line, out string key, out string value)
    {
        string trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] is ';' or '#')
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        int separator = line.IndexOf('=');
        if (separator <= 0)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = line[..separator].Trim();
        value = line[(separator + 1)..].Trim();
        return key.Length > 0;
    }

    private readonly record struct SectionRange(int Start, int End);
}
