using System.IO;
using System.Runtime.InteropServices;

namespace FolderColorizer.Services;

internal static partial class ShellFolderSettings
{
    private const uint IconFileMask = 0x00000010;
    private const uint ForceWrite = 0x00000002;

    public static bool TrySetIcon(
        string folderPath,
        string iconFileName)
    {
        string folder = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(folderPath));
        nint iconFile = 0;
        try
        {
            iconFile = Marshal.StringToCoTaskMemUni(iconFileName);
            var settings = new ShellFolderCustomSettings
            {
                Size = (uint)Marshal.SizeOf<ShellFolderCustomSettings>(),
                Mask = IconFileMask,
                IconFile = iconFile,
                IconIndex = 0
            };

            int result = SHGetSetFolderCustomSettings(
                ref settings,
                folder,
                ForceWrite);

            return result >= 0;
        }
        finally
        {
            if (iconFile != 0)
            {
                Marshal.FreeCoTaskMem(iconFile);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShellFolderCustomSettings
    {
        public uint Size;
        public uint Mask;
        public nint ViewId;
        public nint WebViewTemplate;
        public uint WebViewTemplateLength;
        public nint WebViewTemplateVersion;
        public nint InfoTip;
        public uint InfoTipLength;
        public nint Clsid;
        public uint Flags;
        public nint IconFile;
        public uint IconFileLength;
        public int IconIndex;
        public nint Logo;
        public uint LogoLength;
    }

    [LibraryImport(
        "shell32.dll",
        EntryPoint = "SHGetSetFolderCustomSettings",
        StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHGetSetFolderCustomSettings(
        ref ShellFolderCustomSettings settings,
        string folderPath,
        uint readWrite);
}
