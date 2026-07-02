using System.IO;
using FolderColorizer.Services;
using Microsoft.Win32;
using Xunit;

namespace FolderColorizer.Core.Tests;

public sealed class ContextMenuRegistrarTests
{
    [Fact]
    public void RegistrationCreatesCascadingMenuWithAllCommands()
    {
        string testKeyPath = $@"Software\FolderColorizer.Tests\{Guid.NewGuid():N}";
        using RegistryKey testRoot = Registry.CurrentUser.CreateSubKey(testKeyPath, true);

        try
        {
            const string executablePath = @"C:\Program Files\Folder Colorizer\FolderColorizer.exe";
            const string iconDirectory = @"C:\ProgramData\Folder Colorizer\MenuIcons\1.0.2";
            ContextMenuRegistrar.WriteRegistration(testRoot, executablePath, iconDirectory);

            using RegistryKey parent = testRoot.OpenSubKey(
                @"Directory\shell\FolderColorizer",
                false) ?? throw new InvalidOperationException("Parent menu was not created.");
            using RegistryKey commands = parent.OpenSubKey("shell", false)
                ?? throw new InvalidOperationException("Commands were not created.");

            Assert.Equal("Folder Colorizer", parent.GetValue("MUIVerb"));
            Assert.Equal(string.Empty, parent.GetValue("SubCommands"));
            Assert.Null(parent.GetValue("ExtendedSubCommandsKey"));
            Assert.Equal(13, commands.GetSubKeyNames().Length);

            using RegistryKey redCommand = commands.OpenSubKey(
                @"01.red\command",
                false) ?? throw new InvalidOperationException("Red command was not created.");
            using RegistryKey red = commands.OpenSubKey(
                "01.red",
                false) ?? throw new InvalidOperationException("Red menu was not created.");
            using RegistryKey blue = commands.OpenSubKey(
                "08.blue",
                false) ?? throw new InvalidOperationException("Blue menu was not created.");

            Assert.Equal(
                $"\"{executablePath}\" --color red \"%1\"",
                redCommand.GetValue(null));
            Assert.Equal(
                $"\"{Path.Combine(iconDirectory, "red.ico")}\",0",
                red.GetValue("Icon"));
            Assert.Equal(
                $"\"{Path.Combine(iconDirectory, "blue.ico")}\",0",
                blue.GetValue("Icon"));
            Assert.NotEqual(red.GetValue("Icon"), blue.GetValue("Icon"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, false);
        }
    }
}
