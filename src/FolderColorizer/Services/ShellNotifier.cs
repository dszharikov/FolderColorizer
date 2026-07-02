using System.Runtime.InteropServices;

namespace FolderColorizer.Services;

internal static partial class ShellNotifier
{
    private const uint ShcneUpdateItem = 0x00002000;
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfPathW = 0x0005;
    private const uint ShcnfIdList = 0x0000;
    private const uint ShcnfFlush = 0x1000;
    private const uint ShcnfFlushNowait = 0x2000;

    public static void FolderChanged(string folderPath) =>
        SHChangeNotify(ShcneUpdateItem, ShcnfPathW | ShcnfFlushNowait, folderPath, null);

    public static void AssociationsChanged() =>
        SHChangeNotify(ShcneAssocChanged, ShcnfIdList | ShcnfFlush, null, null);

    [LibraryImport("shell32.dll", EntryPoint = "SHChangeNotify", StringMarshalling = StringMarshalling.Utf16)]
    private static partial void SHChangeNotify(
        uint eventId,
        uint flags,
        string? item1,
        string? item2);
}
