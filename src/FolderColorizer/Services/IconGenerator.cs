using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FolderColorizer.Services;

internal static class IconGenerator
{
    private static readonly int[] IconSizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    public static byte[] Create(FolderColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        List<byte[]> images = IconSizes.Select(size => RenderPng(color, size)).ToList();
        const int headerSize = 6;
        const int directoryEntrySize = 16;
        int imageOffset = headerSize + (directoryEntrySize * images.Count);
        int totalSize = imageOffset + images.Sum(image => image.Length);
        byte[] icon = new byte[totalSize];

        BinaryPrimitives.WriteUInt16LittleEndian(icon.AsSpan(0, 2), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(icon.AsSpan(2, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(icon.AsSpan(4, 2), (ushort)images.Count);

        int currentOffset = imageOffset;
        for (int index = 0; index < images.Count; index++)
        {
            int size = IconSizes[index];
            int entryOffset = headerSize + (index * directoryEntrySize);
            icon[entryOffset] = size == 256 ? (byte)0 : (byte)size;
            icon[entryOffset + 1] = size == 256 ? (byte)0 : (byte)size;
            icon[entryOffset + 2] = 0;
            icon[entryOffset + 3] = 0;
            BinaryPrimitives.WriteUInt16LittleEndian(icon.AsSpan(entryOffset + 4, 2), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(icon.AsSpan(entryOffset + 6, 2), 32);
            BinaryPrimitives.WriteUInt32LittleEndian(
                icon.AsSpan(entryOffset + 8, 4),
                (uint)images[index].Length);
            BinaryPrimitives.WriteUInt32LittleEndian(
                icon.AsSpan(entryOffset + 12, 4),
                (uint)currentOffset);

            images[index].CopyTo(icon, currentOffset);
            currentOffset += images[index].Length;
        }

        return icon;
    }

    private static byte[] RenderPng(FolderColor color, int size)
    {
        const double canvasSize = 256;
        var visual = new DrawingVisual();

        using (DrawingContext drawing = visual.RenderOpen())
        {
            var shadow = new SolidColorBrush(Color.FromArgb(42, 15, 23, 42));
            shadow.Freeze();
            drawing.DrawRoundedRectangle(shadow, null, new Rect(18, 61, 220, 172), 22, 22);

            Color topColor = ParseColor(color.Hex);
            Color bottomColor = ParseColor(color.DarkHex);
            var gradient = new LinearGradientBrush(topColor, bottomColor, new Point(0.2, 0), new Point(0.8, 1));
            gradient.Freeze();

            StreamGeometry folder = CreateFolderGeometry();
            var border = new Pen(new SolidColorBrush(Color.FromArgb(42, 0, 0, 0)), 2.5);
            border.Freeze();
            drawing.DrawGeometry(gradient, border, folder);

            var highlight = new LinearGradientBrush(
                Color.FromArgb(105, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                new Point(0, 0),
                new Point(0, 1));
            highlight.Freeze();
            drawing.DrawRoundedRectangle(highlight, null, new Rect(27, 84, 202, 52), 13, 13);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        visual.Transform = new ScaleTransform(size / canvasSize, size / canvasSize);
        bitmap.Render(visual);
        bitmap.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static StreamGeometry CreateFolderGeometry()
    {
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(18, 58), true, true);
            context.QuadraticBezierTo(new Point(18, 43), new Point(35, 43), true, false);
            context.LineTo(new Point(92, 43), true, false);
            context.QuadraticBezierTo(new Point(99, 43), new Point(104, 49), true, false);
            context.LineTo(new Point(121, 68), true, false);
            context.LineTo(new Point(219, 68), true, false);
            context.QuadraticBezierTo(new Point(238, 68), new Point(238, 87), true, false);
            context.LineTo(new Point(238, 207), true, false);
            context.QuadraticBezierTo(new Point(238, 226), new Point(219, 226), true, false);
            context.LineTo(new Point(37, 226), true, false);
            context.QuadraticBezierTo(new Point(18, 226), new Point(18, 207), true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Color ParseColor(string value) =>
        (Color)System.Windows.Media.ColorConverter.ConvertFromString(value);
}
