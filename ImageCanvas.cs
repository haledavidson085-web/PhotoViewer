using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace PhotoViewer;

internal sealed class ImageCanvas : Control
{
    private const double MinimumZoom = 0.05;
    private const double MaximumZoom = 20;
    private Image? image;
    private double zoom = 1;
    private bool fitToWindow = true;
    private PointF panOffset;
    private Point dragStart;
    private PointF panAtDragStart;
    private bool dragging;

    public ImageCanvas()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.CanvasBackground;
        ForeColor = AppTheme.Foreground;
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? Image
    {
        get => image;
        set
        {
            image = value;
            Fit();
        }
    }

    public bool IsFitToWindow => fitToWindow;

    public double EffectiveZoom
    {
        get
        {
            if (image is null || !fitToWindow || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return zoom;

            return Math.Min((double)ClientSize.Width / image.Width, (double)ClientSize.Height / image.Height);
        }
    }

    public event EventHandler? ViewChanged;

    public void Fit()
    {
        fitToWindow = true;
        panOffset = PointF.Empty;
        Invalidate();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ActualSize()
    {
        fitToWindow = false;
        zoom = 1;
        panOffset = PointF.Empty;
        Invalidate();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ZoomIn() => SetZoom(EffectiveZoom * 1.25);

    public void ZoomOut() => SetZoom(EffectiveZoom / 1.25);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);

        if (image is null)
        {
            using var font = new Font(Font.FontFamily, 16, FontStyle.Regular);
            const string message = "Drop a photo here or press Ctrl+O";
            var size = e.Graphics.MeasureString(message, font);
            using var brush = new SolidBrush(AppTheme.MutedForeground);
            e.Graphics.DrawString(message, font, brush,
                (ClientSize.Width - size.Width) / 2,
                (ClientSize.Height - size.Height) / 2);
            return;
        }

        double scale = EffectiveZoom;
        float width = (float)(image.Width * scale);
        float height = (float)(image.Height * scale);
        float x = (ClientSize.Width - width) / 2f + panOffset.X;
        float y = (ClientSize.Height - height) / 2f + panOffset.Y;
        var imageBounds = new RectangleF(x, y, width, height);

        DrawTransparencyGrid(e.Graphics, imageBounds);
        e.Graphics.InterpolationMode = scale < 1
            ? InterpolationMode.HighQualityBicubic
            : InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.DrawImage(image, imageBounds);
        using var border = new Pen(AppTheme.Border);
        e.Graphics.DrawRectangle(border, x, y, width, height);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left || image is null)
            return;

        dragging = true;
        dragStart = e.Location;
        panAtDragStart = panOffset;
        Cursor = Cursors.Hand;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging)
            return;

        panOffset = new PointF(
            panAtDragStart.X + e.X - dragStart.X,
            panAtDragStart.Y + e.Y - dragStart.Y);
        fitToWindow = false;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragging = false;
        Capture = false;
        Cursor = Cursors.Default;
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (image is null)
            return;

        SetZoom(EffectiveZoom * (e.Delta > 0 ? 1.15 : 1 / 1.15));
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (image is null)
            return;

        if (fitToWindow)
            ActualSize();
        else
            Fit();
    }

    private void SetZoom(double value)
    {
        if (image is null)
            return;

        zoom = Math.Clamp(value, MinimumZoom, MaximumZoom);
        fitToWindow = false;
        Invalidate();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DrawTransparencyGrid(Graphics graphics, RectangleF bounds)
    {
        var visible = RectangleF.Intersect(bounds, ClientRectangle);
        if (visible.Width <= 0 || visible.Height <= 0)
            return;

        const float tileSize = 10f;
        using var first = new SolidBrush(Color.FromArgb(38, 43, 53));
        using var second = new SolidBrush(Color.FromArgb(47, 53, 65));
        var state = graphics.Save();
        graphics.SetClip(visible);

        float startX = (float)Math.Floor(visible.Left / tileSize) * tileSize;
        float startY = (float)Math.Floor(visible.Top / tileSize) * tileSize;
        for (float y = startY; y < visible.Bottom; y += tileSize)
        {
            for (float x = startX; x < visible.Right; x += tileSize)
            {
                int column = (int)Math.Floor((x - bounds.Left) / tileSize);
                int row = (int)Math.Floor((y - bounds.Top) / tileSize);
                graphics.FillRectangle(((column + row) & 1) == 0 ? first : second,
                    x, y, tileSize, tileSize);
            }
        }

        graphics.Restore(state);
    }
}
