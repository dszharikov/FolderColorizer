using FolderColorizer.Core;
using Xunit;

namespace FolderColorizer.Core.Tests;

public sealed class IniDocumentTests
{
    [Fact]
    public void SetValuePreservesUnrelatedContent()
    {
        IniDocument document = IniDocument.Parse(
            "; keep this comment\r\n" +
            "[ViewState]\r\n" +
            "Mode=Tiles\r\n");

        document.SetValue(".ShellClassInfo", "IconFile", "folder.ico");

        Assert.Contains("; keep this comment", document.ToString(), StringComparison.Ordinal);
        Assert.Equal("Tiles", document.GetValue("ViewState", "Mode"));
        Assert.Equal("folder.ico", document.GetValue(".ShellClassInfo", "IconFile"));
    }

    [Fact]
    public void ApplyAndRestoreRoundTripsExistingIconConfiguration()
    {
        IniDocument document = IniDocument.Parse(
            "[.ShellClassInfo]\r\n" +
            "IconResource=C:\\Icons\\original.dll,3\r\n" +
            "InfoTip=Important folder\r\n");

        FolderColorState.Apply(document, folderWasReadOnly: true);
        RestoreResult result = FolderColorState.Restore(document);

        Assert.True(result.WasManaged);
        Assert.True(result.FolderWasReadOnly);
        Assert.Equal(
            "C:\\Icons\\original.dll,3",
            document.GetValue(".ShellClassInfo", "IconResource"));
        Assert.Equal("Important folder", document.GetValue(".ShellClassInfo", "InfoTip"));
        Assert.Null(document.GetValue(".ShellClassInfo", "IconFile"));
        Assert.Null(document.GetValue("FolderColorizer", "Managed"));
    }

    [Fact]
    public void RecolorDoesNotReplaceOriginalBackup()
    {
        IniDocument document = IniDocument.Parse(
            "[.ShellClassInfo]\r\n" +
            "IconFile=before.ico\r\n" +
            "IconIndex=7\r\n");

        FolderColorState.Apply(document, folderWasReadOnly: false);
        FolderColorState.Apply(document, folderWasReadOnly: true);
        RestoreResult result = FolderColorState.Restore(document);

        Assert.False(result.FolderWasReadOnly);
        Assert.Equal("before.ico", document.GetValue(".ShellClassInfo", "IconFile"));
        Assert.Equal("7", document.GetValue(".ShellClassInfo", "IconIndex"));
    }

    [Fact]
    public void RestoreUnmanagedDocumentDoesNothing()
    {
        IniDocument document = IniDocument.Parse("[.ShellClassInfo]\r\nInfoTip=Keep me\r\n");

        RestoreResult result = FolderColorState.Restore(document);

        Assert.False(result.WasManaged);
        Assert.Equal("Keep me", document.GetValue(".ShellClassInfo", "InfoTip"));
    }
}
