namespace FolderColorizer;

public sealed record FolderColor(
    string Id,
    string DisplayName,
    string Hex,
    string DarkHex);

public static class FolderPalette
{
    public static IReadOnlyList<FolderColor> All { get; } =
    [
        new("red", "Red", "#EF5350", "#C62828"),
        new("orange", "Orange", "#FF8A3D", "#E65100"),
        new("amber", "Amber", "#FFC83D", "#F59E0B"),
        new("lime", "Lime", "#9CCC45", "#558B2F"),
        new("green", "Green", "#42B883", "#16865B"),
        new("teal", "Teal", "#26A69A", "#00796B"),
        new("cyan", "Cyan", "#29B6D1", "#0086A3"),
        new("blue", "Blue", "#4285F4", "#2459B8"),
        new("indigo", "Indigo", "#6574CD", "#3949AB"),
        new("violet", "Violet", "#9C6ADE", "#6A3DB4"),
        new("pink", "Pink", "#EC6F9E", "#C2366F"),
        new("slate", "Slate", "#78909C", "#455A64")
    ];

    public static FolderColor? Find(string id) =>
        All.FirstOrDefault(color => color.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
