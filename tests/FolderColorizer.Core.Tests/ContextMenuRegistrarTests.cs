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
            ContextMenuRegistrar.WriteRegistration(testRoot, executablePath);

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
            Assert.Equal(
                $"\"{executablePath}\" --color red \"%1\"",
                redCommand.GetValue(null));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, false);
        }
    }
}
