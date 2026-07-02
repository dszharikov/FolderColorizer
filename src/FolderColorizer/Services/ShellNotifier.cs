using System.Runtime.InteropServices;

namespace FolderColorizer.Services;

internal static partial class ShellNotifier
{
    private const uint ShcneUpdateItem = 0x00002000;
    private const uint ShcnfPathW = 0x0005;
    private const uint ShcnfFlushNowait = 0x2000;

    public static void FolderChanged(string folderPath) =>
        SHChangeNotify(ShcneUpdateItem, ShcnfPathW | ShcnfFlushNowait, folderPath, null);

    [LibraryImport("shell32.dll", EntryPoint = "SHChangeNotify", StringMarshalling = StringMarshalling.Utf16)]
    private static partial void SHChangeNotify(
        uint eventId,
        uint flags,
        string? item1,
        string? item2);
}
