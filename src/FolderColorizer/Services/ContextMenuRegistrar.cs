using System.IO;
using Microsoft.Win32;

namespace FolderColorizer.Services;

internal static class ContextMenuRegistrar
{
    private const string ProductDataDirectoryName = "FolderColorizer";
    private const string MenuIconsDirectoryName = "MenuIcons";
    private const string ClassesRootKey = @"Software\Classes";
    private const string DirectoryMenuKey = @"Directory\shell\FolderColorizer";
    private const string SubCommandsKey = DirectoryMenuKey + @"\shell";
    private const string ObsoleteContextMenuClassKey = "FolderColorizer.ContextMenu";
    private const string LegacyCommandStoreRoot =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";

    public static void Register(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        string fullPath = Path.GetFullPath(executablePath);
        string iconDirectory = EnsureMenuIcons();
        using RegistryKey classesRoot = Registry.CurrentUser.CreateSubKey(ClassesRootKey, true);
        WriteRegistration(classesRoot, fullPath, iconDirectory);
        DeleteLegacyCommandStoreCommands();
        ShellNotifier.AssociationsChanged();
    }

    public static void Unregister()
    {
        using RegistryKey classesRoot = Registry.CurrentUser.CreateSubKey(ClassesRootKey, true);
        DeleteRegistration(classesRoot);
        DeleteLegacyCommandStoreCommands();
        DeleteMenuIcons();
        ShellNotifier.AssociationsChanged();
    }

    internal static void WriteRegistration(
        RegistryKey classesRoot,
        string executablePath,
        string iconDirectory)
    {
        ArgumentNullException.ThrowIfNull(classesRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconDirectory);

        string fullPath = Path.GetFullPath(executablePath);
        string fullIconDirectory = Path.GetFullPath(iconDirectory);
        string icon = $"\"{fullPath}\",0";
        DeleteRegistration(classesRoot);
        CreateParentMenu(classesRoot, DirectoryMenuKey, icon);

        for (int index = 0; index < FolderPalette.All.Count; index++)
        {
            FolderColor color = FolderPalette.All[index];
            string colorIcon = FormatIconPath(
                Path.Combine(fullIconDirectory, $"{color.Id}.ico"));
            CreateCommand(
                classesRoot,
                $"{index + 1:D2}.{color.Id}",
                color.DisplayName,
                colorIcon,
                $"\"{fullPath}\" --color {color.Id} \"%1\"",
                separatorBefore: false);
        }

        string defaultIcon = FormatIconPath(
            Path.Combine(fullIconDirectory, $"{FolderPalette.DefaultFolder.Id}.ico"));
        CreateCommand(
            classesRoot,
            "99.reset",
            "Restore default",
            defaultIcon,
            $"\"{fullPath}\" --reset \"%1\"",
            separatorBefore: true);
    }

    internal static void DeleteRegistration(RegistryKey classesRoot)
    {
        ArgumentNullException.ThrowIfNull(classesRoot);
        classesRoot.DeleteSubKeyTree(DirectoryMenuKey, false);
        classesRoot.DeleteSubKeyTree(ObsoleteContextMenuClassKey, false);
    }

    private static void CreateParentMenu(
        RegistryKey classesRoot,
        string keyPath,
        string icon)
    {
        using RegistryKey key = classesRoot.CreateSubKey(keyPath, true);
        key.SetValue("MUIVerb", "Folder Colorizer");
        key.SetValue("Icon", icon);
        key.SetValue("SubCommands", string.Empty);
    }

    private static void CreateCommand(
        RegistryKey classesRoot,
        string id,
        string displayName,
        string icon,
        string command,
        bool separatorBefore)
    {
        string keyPath = $@"{SubCommandsKey}\{id}";
        using RegistryKey key = classesRoot.CreateSubKey(keyPath, true);
        key.SetValue("MUIVerb", displayName);
        key.SetValue("Icon", icon);

        if (separatorBefore)
        {
            key.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord);
        }

        using RegistryKey commandKey = key.CreateSubKey("command", true);
        commandKey.SetValue(null, command);
    }

    private static void DeleteLegacyCommandStoreCommands()
    {
        foreach (FolderColor color in FolderPalette.All)
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"{LegacyCommandStoreRoot}\FolderColorizer.{color.Id}",
                false);
        }

        Registry.CurrentUser.DeleteSubKeyTree(
            $@"{LegacyCommandStoreRoot}\FolderColorizer.reset",
            false);
    }

    private static string EnsureMenuIcons()
    {
        string root = GetMenuIconsRoot();
        string version = typeof(ContextMenuRegistrar).Assembly.GetName().Version?.ToString(3)
            ?? "current";
        string directory = Path.Combine(root, version);
        Directory.CreateDirectory(directory);

        foreach (FolderColor color in FolderPalette.All.Append(FolderPalette.DefaultFolder))
        {
            WriteFileAtomically(
                Path.Combine(directory, $"{color.Id}.ico"),
                IconGenerator.Create(color));
        }

        foreach (string obsoleteDirectory in Directory.EnumerateDirectories(root))
        {
            if (obsoleteDirectory.Equals(directory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TryDeleteDirectory(obsoleteDirectory);
        }

        return directory;
    }

    private static void DeleteMenuIcons() => TryDeleteDirectory(GetMenuIconsRoot());

    private static string GetMenuIconsRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductDataDirectoryName,
            MenuIconsDirectoryName);

    private static string FormatIconPath(string path) => $"\"{path}\",0";

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

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            // Explorer can briefly hold an icon file open. A later registration
            // or uninstall will retry cleanup without breaking the menu update.
        }
        catch (UnauthorizedAccessException)
        {
            // Stale icon files are harmless and should not block unregistration.
        }
    }
}
