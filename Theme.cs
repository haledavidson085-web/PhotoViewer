using System.Runtime.InteropServices;

namespace PhotoViewer;

internal static class AppTheme
{
    public static Color WindowBackground { get; private set; } = Color.FromArgb(22, 25, 31);
    public static Color CanvasBackground { get; private set; } = Color.FromArgb(17, 20, 26);
    public static Color Surface { get; private set; } = Color.FromArgb(31, 35, 44);
    public static Color SurfaceRaised { get; private set; } = Color.FromArgb(39, 44, 55);
    public static Color SurfaceHover { get; private set; } = Color.FromArgb(49, 56, 69);
    public static Color SurfacePressed { get; private set; } = Color.FromArgb(58, 66, 82);
    public static Color Border { get; private set; } = Color.FromArgb(62, 70, 84);
    public static Color Foreground { get; private set; } = Color.FromArgb(235, 239, 246);
    public static Color MutedForeground { get; private set; } = Color.FromArgb(156, 166, 182);
    public static Color Accent { get; private set; } = Color.FromArgb(72, 199, 255);
    public static Color AccentSecondary { get; private set; } = Color.FromArgb(151, 103, 255);
    public static Color WarmAccent { get; private set; } = Color.FromArgb(255, 177, 66);
    public static Color CheckerLight { get; private set; } = Color.FromArgb(47, 53, 65);
    public static Color CheckerDark { get; private set; } = Color.FromArgb(38, 43, 53);

    public static void Apply(AppSettings settings)
    {
        WindowBackground = Color.FromArgb(settings.WindowBackgroundArgb);
        CanvasBackground = Color.FromArgb(settings.CanvasBackgroundArgb);
        Surface = Color.FromArgb(settings.SurfaceArgb);
        Foreground = Color.FromArgb(settings.ForegroundArgb);
        Accent = Color.FromArgb(settings.AccentArgb);
        AccentSecondary = Color.FromArgb(settings.SecondaryAccentArgb);

        bool lightSurface = IsLight(Surface);
        Color contrast = lightSurface ? Color.Black : Color.White;
        SurfaceRaised = Blend(Surface, contrast, 0.06f);
        SurfaceHover = Blend(Surface, contrast, 0.12f);
        SurfacePressed = Blend(Surface, contrast, 0.18f);
        Border = Blend(Surface, Foreground, 0.20f);
        MutedForeground = Blend(Surface, Foreground, 0.65f);
        bool lightCanvas = IsLight(CanvasBackground);
        CheckerDark = Blend(CanvasBackground, lightCanvas ? Color.Black : Color.White, 0.08f);
        CheckerLight = Blend(CanvasBackground, lightCanvas ? Color.Black : Color.White, 0.15f);
    }

    public static Color Blend(Color first, Color second, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            255,
            (int)Math.Round(first.R + (second.R - first.R) * amount),
            (int)Math.Round(first.G + (second.G - first.G) * amount),
            (int)Math.Round(first.B + (second.B - first.B) * amount));
    }

    public static ToolStripRenderer CreateRenderer() => new DarkToolStripRenderer();

    public static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        int enabled = IsLight(WindowBackground) ? 0 : 1;
        if (DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
    }

    private static bool IsLight(Color color) =>
        (color.R * 299 + color.G * 587 + color.B * 114) / 1000 >= 150;

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
