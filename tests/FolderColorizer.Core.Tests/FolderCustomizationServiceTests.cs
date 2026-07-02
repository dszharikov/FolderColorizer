using System.IO;
using FolderColorizer.Services;
using Xunit;

namespace FolderColorizer.Core.Tests;

public sealed class FolderCustomizationServiceTests
{
    [Fact]
    public void ApplyAndResetRestoresExistingDesktopIni()
    {
        string folder = Path.Combine(
            Path.GetTempPath(),
            "FolderColorizer.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        try
        {
            string desktopIni = Path.Combine(folder, "desktop.ini");
            File.WriteAllText(
                desktopIni,
                "[.ShellClassInfo]\r\n" +
                "IconResource=C:\\Icons\\original.dll,3\r\n" +
                "InfoTip=Keep me\r\n",
                new System.Text.UnicodeEncoding(false, true));
            string legacyIcon = Path.Combine(folder, ".foldercolorizer.ico");
            File.WriteAllBytes(legacyIcon, [0]);

            FolderCustomizationService.Apply(folder, FolderPalette.All[0]);
            string redIcon = Path.Combine(folder, ".foldercolorizer.red.ico");
            Assert.True(File.Exists(redIcon));
            Assert.False(File.Exists(legacyIcon));

            FolderCustomizationService.Apply(folder, FolderPalette.Find("blue")!);
            string blueIcon = Path.Combine(folder, ".foldercolorizer.blue.ico");
            Assert.True(File.Exists(blueIcon));
            Assert.False(File.Exists(redIcon));
            Assert.Contains(
                "IconResource=.foldercolorizer.blue.ico,0",
                File.ReadAllText(desktopIni, System.Text.Encoding.Unicode));

            bool changed = FolderCustomizationService.Reset(folder);

            string restored = File.ReadAllText(desktopIni, System.Text.Encoding.Unicode);
            Assert.True(changed);
            Assert.Contains("IconResource=C:\\Icons\\original.dll,3", restored);
            Assert.Contains("InfoTip=Keep me", restored);
            Assert.DoesNotContain("FolderColorizer", restored);
            Assert.False(File.Exists(blueIcon));
            Assert.False(File.GetAttributes(folder).HasFlag(FileAttributes.ReadOnly));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                File.SetAttributes(folder, FileAttributes.Directory);
                Directory.Delete(folder, true);
            }
        }
    }
}
