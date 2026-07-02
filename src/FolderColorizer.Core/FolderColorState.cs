using System.Text;

namespace FolderColorizer.Core;

public static class FolderColorState
{
    public const string ShellSection = ".ShellClassInfo";
    public const string StateSection = "FolderColorizer";
    public const string IconFileName = ".foldercolorizer.ico";

    private static readonly string[] ManagedKeys = ["IconResource", "IconFile", "IconIndex"];

    public static void Apply(IniDocument document, bool folderWasReadOnly)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!string.Equals(
                document.GetValue(StateSection, "Managed"),
                "1",
                StringComparison.Ordinal))
        {
            document.SetValue(StateSection, "Managed", "1");
            document.SetValue(
                StateSection,
                "OriginalFolderReadOnly",
                folderWasReadOnly ? "1" : "0");

            foreach (string key in ManagedKeys)
            {
                string? original = document.GetValue(ShellSection, key);
                document.SetValue(
                    StateSection,
                    $"Had{key}",
                    original is null ? "0" : "1");

                if (original is not null)
                {
                    document.SetValue(
                        StateSection,
                        $"Original{key}",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(original)));
                }
            }
        }

        document.SetValue(ShellSection, "IconResource", $"{IconFileName},0");
        document.SetValue(ShellSection, "IconFile", IconFileName);
        document.SetValue(ShellSection, "IconIndex", "0");
    }

    public static RestoreResult Restore(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!string.Equals(
                document.GetValue(StateSection, "Managed"),
                "1",
                StringComparison.Ordinal))
        {
            return new RestoreResult(false, false);
        }

        foreach (string key in ManagedKeys)
        {
            bool hadValue = string.Equals(
                document.GetValue(StateSection, $"Had{key}"),
                "1",
                StringComparison.Ordinal);

            if (hadValue)
            {
                string encoded = document.GetValue(StateSection, $"Original{key}")
                    ?? throw new InvalidDataException($"The saved {key} value is missing.");
                string original = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                document.SetValue(ShellSection, key, original);
            }
            else
            {
                document.RemoveValue(ShellSection, key);
            }
        }

        bool folderWasReadOnly = string.Equals(
            document.GetValue(StateSection, "OriginalFolderReadOnly"),
            "1",
            StringComparison.Ordinal);
        document.RemoveSection(StateSection);

        return new RestoreResult(true, folderWasReadOnly);
    }
}

public readonly record struct RestoreResult(bool WasManaged, bool FolderWasReadOnly);
