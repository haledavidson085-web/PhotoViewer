using System.Runtime.InteropServices;

namespace PhotoViewer;

internal static class AppTheme
{
    public static readonly Color WindowBackground = Color.FromArgb(22, 25, 31);
    public static readonly Color CanvasBackground = Color.FromArgb(17, 20, 26);
    public static readonly Color Surface = Color.FromArgb(31, 35, 44);
    public static readonly Color SurfaceRaised = Color.FromArgb(39, 44, 55);
    public static readonly Color SurfaceHover = Color.FromArgb(49, 56, 69);
    public static readonly Color SurfacePressed = Color.FromArgb(58, 66, 82);
    public static readonly Color Border = Color.FromArgb(62, 70, 84);
    public static readonly Color Foreground = Color.FromArgb(235, 239, 246);
    public static readonly Color MutedForeground = Color.FromArgb(156, 166, 182);
    public static readonly Color Accent = Color.FromArgb(72, 199, 255);
    public static readonly Color AccentSecondary = Color.FromArgb(151, 103, 255);
    public static readonly Color WarmAccent = Color.FromArgb(255, 177, 66);

    public static ToolStripRenderer CreateRenderer() => new DarkToolStripRenderer();

    public static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        int enabled = 1;
        if (DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}

internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? AppTheme.Foreground : AppTheme.MutedForeground;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled != false ? AppTheme.Foreground : AppTheme.MutedForeground;
        base.OnRenderArrow(e);
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public DarkColorTable()
    {
        UseSystemColors = false;
    }

    public override Color ToolStripDropDownBackground => AppTheme.Surface;
    public override Color ImageMarginGradientBegin => AppTheme.Surface;
    public override Color ImageMarginGradientMiddle => AppTheme.Surface;
    public override Color ImageMarginGradientEnd => AppTheme.Surface;
    public override Color MenuBorder => AppTheme.Border;
    public override Color MenuItemBorder => AppTheme.Border;
    public override Color MenuItemSelected => AppTheme.SurfaceHover;
    public override Color MenuStripGradientBegin => AppTheme.Surface;
    public override Color MenuStripGradientEnd => AppTheme.Surface;
    public override Color MenuItemSelectedGradientBegin => AppTheme.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => AppTheme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => AppTheme.SurfacePressed;
    public override Color MenuItemPressedGradientMiddle => AppTheme.SurfacePressed;
    public override Color MenuItemPressedGradientEnd => AppTheme.SurfacePressed;
    public override Color ButtonSelectedBorder => AppTheme.Border;
    public override Color ButtonSelectedGradientBegin => AppTheme.SurfaceHover;
    public override Color ButtonSelectedGradientMiddle => AppTheme.SurfaceHover;
    public override Color ButtonSelectedGradientEnd => AppTheme.SurfaceHover;
    public override Color ButtonPressedBorder => AppTheme.Accent;
    public override Color ButtonPressedGradientBegin => AppTheme.SurfacePressed;
    public override Color ButtonPressedGradientMiddle => AppTheme.SurfacePressed;
    public override Color ButtonPressedGradientEnd => AppTheme.SurfacePressed;
    public override Color ButtonCheckedGradientBegin => AppTheme.SurfacePressed;
    public override Color ButtonCheckedGradientMiddle => AppTheme.SurfacePressed;
    public override Color ButtonCheckedGradientEnd => AppTheme.SurfacePressed;
    public override Color ToolStripBorder => AppTheme.Border;
    public override Color ToolStripGradientBegin => AppTheme.Surface;
    public override Color ToolStripGradientMiddle => AppTheme.Surface;
    public override Color ToolStripGradientEnd => AppTheme.Surface;
    public override Color StatusStripGradientBegin => AppTheme.Surface;
    public override Color StatusStripGradientEnd => AppTheme.Surface;
    public override Color SeparatorDark => AppTheme.Border;
    public override Color SeparatorLight => AppTheme.SurfaceRaised;
}
