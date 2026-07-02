using System.IO;
using System.Text;
using FolderColorizer.Core;

namespace FolderColorizer.Services;

public static class FolderCustomizationService
{
    private const string DesktopIniName = "desktop.ini";
    private static readonly UnicodeEncoding UnicodeWithBom = new(false, true, true);

    public static void Apply(string folderPath, FolderColor color)
    {
        ArgumentNullException.ThrowIfNull(color);
        string folder = ValidateFolder(folderPath);
        string desktopIniPath = Path.Combine(folder, DesktopIniName);
        string iconFileName = FolderColorState.GetIconFileName(color.Id);
        string iconPath = Path.Combine(folder, iconFileName);

        FileAttributes folderAttributes = File.GetAttributes(folder);
        bool folderWasReadOnly = folderAttributes.HasFlag(FileAttributes.ReadOnly);
        IniDocument document = ReadDesktopIni(desktopIniPath);
        FolderColorState.Apply(document, folderWasReadOnly, iconFileName);

        File.SetAttributes(folder, folderAttributes & ~FileAttributes.ReadOnly);
        try
        {
            WriteFileAtomically(iconPath, IconGenerator.Create(color));
            SetHiddenSystemAttributes(iconPath);
            WriteDesktopIni(desktopIniPath, document);
            SetHiddenSystemAttributes(desktopIniPath);
        }
        finally
        {
            File.SetAttributes(folder, folderAttributes | FileAttributes.ReadOnly);
        }

        DeleteManagedIcons(folder, iconPath);
        ShellNotifier.FolderChanged(folder);
        ExplorerWindowRefresher.RefreshAll();
    }

    public static bool Reset(string folderPath)
    {
        string folder = ValidateFolder(folderPath);
        string desktopIniPath = Path.Combine(folder, DesktopIniName);
        if (!File.Exists(desktopIniPath))
        {
            return false;
        }

        IniDocument document = ReadDesktopIni(desktopIniPath);
        RestoreResult result = FolderColorState.Restore(document);
        if (!result.WasManaged)
        {
            return false;
        }

        FileAttributes attributes = File.GetAttributes(folder);
        File.SetAttributes(folder, attributes & ~FileAttributes.ReadOnly);
        try
        {
            if (document.HasValues())
            {
                WriteDesktopIni(desktopIniPath, document);
                SetHiddenSystemAttributes(desktopIniPath);
            }
            else
            {
                DeleteFileIgnoringAttributes(desktopIniPath);
            }

            DeleteManagedIcons(folder, keepPath: null);
        }
        finally
        {
            attributes = result.FolderWasReadOnly
                ? attributes | FileAttributes.ReadOnly
                : attributes & ~FileAttributes.ReadOnly;
            File.SetAttributes(folder, attributes);
        }

        ShellNotifier.FolderChanged(folder);
        ExplorerWindowRefresher.RefreshAll();
        return true;
    }

    private static string ValidateFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        string fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath.Trim()));

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Folder not found:{Environment.NewLine}{fullPath}");
        }

        return fullPath;
    }

    private static IniDocument ReadDesktopIni(string path)
    {
        if (!File.Exists(path))
        {
            return IniDocument.Empty();
        }

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return IniDocument.Parse(Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2));
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return IniDocument.Parse(Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
        }

        return IniDocument.Parse(Encoding.Default.GetString(bytes));
    }

    private static void WriteDesktopIni(string path, IniDocument document)
    {
        ClearSpecialFileAttributes(path);
        byte[] preamble = UnicodeWithBom.GetPreamble();
        byte[] text = UnicodeWithBom.GetBytes(document.ToString());
        byte[] content = new byte[preamble.Length + text.Length];
        preamble.CopyTo(content, 0);
        text.CopyTo(content, preamble.Length);
        WriteFileAtomically(path, content);
    }

    private static void WriteFileAtomically(string destination, byte[] content)
    {
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static void SetHiddenSystemAttributes(string path) =>
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden | FileAttributes.System);

    private static void DeleteFileIgnoringAttributes(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        ClearSpecialFileAttributes(path);
        File.Delete(path);
    }

    private static void ClearSpecialFileAttributes(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        FileAttributes attributes = File.GetAttributes(path);
        attributes &= ~(FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
        File.SetAttributes(path, attributes);
    }

    private static void DeleteManagedIcons(string folder, string? keepPath)
    {
        var candidates = Directory.EnumerateFiles(
                folder,
                $"{FolderColorState.IconFilePrefix}*{FolderColorState.IconFileSuffix}",
                SearchOption.TopDirectoryOnly)
            .Append(Path.Combine(folder, FolderColorState.LegacyIconFileName));

        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (keepPath is not null &&
                candidate.Equals(keepPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            DeleteFileIgnoringAttributes(candidate);
        }
    }
}
