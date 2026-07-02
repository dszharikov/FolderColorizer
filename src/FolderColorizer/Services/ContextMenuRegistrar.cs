using System.IO;
using Microsoft.Win32;

namespace FolderColorizer.Services;

internal static class ContextMenuRegistrar
{
    private const string DirectoryMenuKey = @"Software\Classes\Directory\shell\FolderColorizer";
    private const string SubCommandsKey = DirectoryMenuKey + @"\ExtendedSubCommandsKey\shell";
    private const string LegacyCommandStoreRoot =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";

    public static void Register(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        string fullPath = Path.GetFullPath(executablePath);
        string icon = $"\"{fullPath}\",0";
        DeleteLegacyCommandStoreCommands();
        CreateParentMenu(DirectoryMenuKey, icon);

        for (int index = 0; index < FolderPalette.All.Count; index++)
        {
            FolderColor color = FolderPalette.All[index];
            CreateCommand(
                $"{index + 1:D2}.{color.Id}",
                color.DisplayName,
                icon,
                $"\"{fullPath}\" --color {color.Id} \"%1\"",
                separatorBefore: false);
        }

        CreateCommand(
            "99.reset",
            "Restore default",
            icon,
            $"\"{fullPath}\" --reset \"%1\"",
            separatorBefore: true);
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree(DirectoryMenuKey, false);
        DeleteLegacyCommandStoreCommands();
    }

    private static void CreateParentMenu(string keyPath, string icon)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, true);
        key.SetValue("MUIVerb", "Folder Colorizer");
        key.SetValue("Icon", icon);
        key.SetValue("Position", "Top");
    }

    private static void CreateCommand(
        string id,
        string displayName,
        string icon,
        string command,
        bool separatorBefore)
    {
        string keyPath = $@"{SubCommandsKey}\{id}";
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, true);
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
