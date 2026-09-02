using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PhotoViewer;

internal enum ToolbarIconKind
{
    OpenImage,
    OpenFolder,
    Previous,
    Next,
    ZoomOut,
    ZoomIn,
    Fit,
    ActualSize,
    RotateLeft,
    RotateRight,
    Slideshow
}

internal static class ToolbarIconFactory
{
    public static Image Create(ToolbarIconKind kind, int size = 20)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.ScaleTransform(size / 24f, size / 24f);

        using var primary = CreatePen(AppTheme.Accent);
        using var secondary = CreatePen(AppTheme.AccentSecondary);
        using var foreground = CreatePen(AppTheme.Foreground);
        using var primaryBrush = new SolidBrush(Color.FromArgb(54, AppTheme.Accent));
        using var secondaryBrush = new SolidBrush(Color.FromArgb(70, AppTheme.AccentSecondary));
        using var warmBrush = new SolidBrush(AppTheme.WarmAccent);

        switch (kind)
        {
            case ToolbarIconKind.OpenImage:
                graphics.FillRectangle(primaryBrush, 3.5f, 4.5f, 17, 15);
                graphics.DrawRoundedRectangle(primary, 3.5f, 4.5f, 17, 15, 2.5f);
                graphics.FillEllipse(warmBrush, 15.5f, 7f, 2.8f, 2.8f);
                graphics.DrawLines(secondary,
                [
                    new PointF(5.5f, 17f),
                    new PointF(10f, 11.5f),
                    new PointF(13.2f, 15f),
                    new PointF(15.5f, 12.7f),
                    new PointF(18.5f, 17f)
                ]);
                break;

            case ToolbarIconKind.OpenFolder:
                PointF[] folder =
                [
                    new(3f, 7f), new(9f, 7f), new(11f, 9f), new(21f, 9f),
                    new(19.5f, 19f), new(3f, 19f)
                ];
                graphics.FillPolygon(primaryBrush, folder);
                graphics.DrawPolygon(primary, folder);
                graphics.DrawLine(secondary, 4f, 11f, 19.5f, 11f);
                break;

            case ToolbarIconKind.Previous:
                graphics.DrawLine(foreground, 6f, 5f, 6f, 19f);
                graphics.DrawLines(primary, [new PointF(17f, 5f), new PointF(9f, 12f), new PointF(17f, 19f)]);
                break;

            case ToolbarIconKind.Next:
                graphics.DrawLine(foreground, 18f, 5f, 18f, 19f);
                graphics.DrawLines(primary, [new PointF(7f, 5f), new PointF(15f, 12f), new PointF(7f, 19f)]);
                break;

            case ToolbarIconKind.ZoomOut:
                DrawMagnifier(graphics, primary, foreground, false);
                break;

            case ToolbarIconKind.ZoomIn:
                DrawMagnifier(graphics, primary, foreground, true);
                break;

            case ToolbarIconKind.Fit:
                DrawCorner(graphics, primary, 4f, 9f, 4f, 4f, 9f, 4f);
                DrawCorner(graphics, primary, 15f, 4f, 20f, 4f, 20f, 9f);
                DrawCorner(graphics, secondary, 4f, 15f, 4f, 20f, 9f, 20f);
                DrawCorner(graphics, secondary, 15f, 20f, 20f, 20f, 20f, 15f);
                break;

            case ToolbarIconKind.ActualSize:
                graphics.FillRectangle(primaryBrush, 4f, 4f, 16f, 16f);
                graphics.DrawRectangle(primary, 4f, 4f, 16f, 16f);
                graphics.DrawLine(secondary, 12f, 5f, 12f, 19f);
                graphics.DrawLine(secondary, 5f, 12f, 19f, 12f);
                graphics.FillRectangle(warmBrush, 8.5f, 8.5f, 3f, 3f);
                break;

            case ToolbarIconKind.RotateLeft:
                graphics.DrawArc(primary, 5f, 5f, 14f, 14f, 35f, 285f);
                graphics.FillPolygon(secondaryBrush, [new PointF(4.5f, 5f), new PointF(10f, 5.3f), new PointF(6.8f, 9.5f)]);
                break;

            case ToolbarIconKind.RotateRight:
                graphics.DrawArc(primary, 5f, 5f, 14f, 14f, 220f, 285f);
                graphics.FillPolygon(secondaryBrush, [new PointF(19.5f, 5f), new PointF(14f, 5.3f), new PointF(17.2f, 9.5f)]);
                break;

            case ToolbarIconKind.Slideshow:
                graphics.FillRectangle(primaryBrush, 3.5f, 5f, 17f, 14f);
                graphics.DrawRoundedRectangle(primary, 3.5f, 5f, 17f, 14f, 2.5f);
                graphics.FillPolygon(warmBrush, [new PointF(10f, 8.5f), new PointF(16f, 12f), new PointF(10f, 15.5f)]);
                break;
        }

        return bitmap;
    }

    private static Pen CreatePen(Color color)
    {
        return new Pen(color, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }

    private static void DrawMagnifier(Graphics graphics, Pen ring, Pen symbol, bool plus)
    {
        graphics.DrawEllipse(ring, 3.5f, 3.5f, 12f, 12f);
        graphics.DrawLine(ring, 14f, 14f, 20.5f, 20.5f);
        graphics.DrawLine(symbol, 6.5f, 9.5f, 12.5f, 9.5f);
        if (plus)
            graphics.DrawLine(symbol, 9.5f, 6.5f, 9.5f, 12.5f);
    }

    private static void DrawCorner(Graphics graphics, Pen pen,
        float firstX, float firstY, float cornerX, float cornerY, float lastX, float lastY)
    {
        graphics.DrawLines(pen,
        [
            new PointF(firstX, firstY),
            new PointF(cornerX, cornerY),
            new PointF(lastX, lastY)
        ]);
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen,
        float x, float y, float width, float height, float radius)
    {
        using var path = CreateRoundedRectangle(x, y, width, height, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedRectangle(float x, float y, float width, float height, float radius)
    {
        float diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
