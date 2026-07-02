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

            FolderCustomizationService.Apply(folder, FolderPalette.All[0]);
            bool changed = FolderCustomizationService.Reset(folder);

            string restored = File.ReadAllText(desktopIni, System.Text.Encoding.Unicode);
            Assert.True(changed);
            Assert.Contains("IconResource=C:\\Icons\\original.dll,3", restored);
            Assert.Contains("InfoTip=Keep me", restored);
            Assert.DoesNotContain("FolderColorizer", restored);
            Assert.False(File.Exists(Path.Combine(folder, ".foldercolorizer.ico")));
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
