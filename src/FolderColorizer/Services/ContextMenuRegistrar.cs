using System.IO;
using Microsoft.Win32;

namespace FolderColorizer.Services;

internal static class ContextMenuRegistrar
{
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
        using RegistryKey classesRoot = Registry.CurrentUser.CreateSubKey(ClassesRootKey, true);
        WriteRegistration(classesRoot, fullPath);
        DeleteLegacyCommandStoreCommands();
        ShellNotifier.AssociationsChanged();
    }

    public static void Unregister()
    {
        using RegistryKey classesRoot = Registry.CurrentUser.CreateSubKey(ClassesRootKey, true);
        DeleteRegistration(classesRoot);
        DeleteLegacyCommandStoreCommands();
        ShellNotifier.AssociationsChanged();
    }

    internal static void WriteRegistration(RegistryKey classesRoot, string executablePath)
    {
        ArgumentNullException.ThrowIfNull(classesRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        string fullPath = Path.GetFullPath(executablePath);
        string icon = $"\"{fullPath}\",0";
        DeleteRegistration(classesRoot);
        CreateParentMenu(classesRoot, DirectoryMenuKey, icon);

        for (int index = 0; index < FolderPalette.All.Count; index++)
        {
            FolderColor color = FolderPalette.All[index];
            CreateCommand(
                classesRoot,
                $"{index + 1:D2}.{color.Id}",
                color.DisplayName,
                icon,
                $"\"{fullPath}\" --color {color.Id} \"%1\"",
                separatorBefore: false);
        }

        CreateCommand(
            classesRoot,
            "99.reset",
            "Restore default",
            icon,
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
}
