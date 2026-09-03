using System.Text.Json;

namespace PhotoViewer;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public string FontFamily { get; set; } = "Segoe UI";
    public float FontSize { get; set; } = 9f;
    public int FontStyle { get; set; } = (int)System.Drawing.FontStyle.Regular;
    public int WindowBackgroundArgb { get; set; } = Color.FromArgb(22, 25, 31).ToArgb();
    public int CanvasBackgroundArgb { get; set; } = Color.FromArgb(17, 20, 26).ToArgb();
    public int SurfaceArgb { get; set; } = Color.FromArgb(31, 35, 44).ToArgb();
    public int ForegroundArgb { get; set; } = Color.FromArgb(235, 239, 246).ToArgb();
    public int AccentArgb { get; set; } = Color.FromArgb(72, 199, 255).ToArgb();
    public int SecondaryAccentArgb { get; set; } = Color.FromArgb(151, 103, 255).ToArgb();
    public int SlideshowIntervalSeconds { get; set; } = 3;

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoViewer", "settings.json");

    public static AppSettings Default => new();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (settings is not null)
                    return settings.Normalize();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Invalid or inaccessible settings should never prevent the viewer from starting.
        }

        return Default;
    }

    public bool TrySave(out string? error)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Normalize(), SerializerOptions));
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            return false;
        }
    }

    public Font CreateFont()
    {
        Font systemFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        string family = string.IsNullOrWhiteSpace(FontFamily)
            ? systemFont.FontFamily.Name
            : FontFamily;
        var style = (System.Drawing.FontStyle)FontStyle;

        try
        {
            return new Font(family, Math.Clamp(FontSize, 8f, 20f), style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return new Font(systemFont.FontFamily, Math.Clamp(FontSize, 8f, 20f),
                System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public AppSettings Normalize()
    {
        FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "Segoe UI" : FontFamily.Trim();
        FontSize = Math.Clamp(FontSize, 8f, 20f);
        FontStyle &= (int)(System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic |
            System.Drawing.FontStyle.Underline | System.Drawing.FontStyle.Strikeout);
        SlideshowIntervalSeconds = Math.Clamp(SlideshowIntervalSeconds, 1, 60);
        WindowBackgroundArgb = MakeOpaque(WindowBackgroundArgb);
        CanvasBackgroundArgb = MakeOpaque(CanvasBackgroundArgb);
        SurfaceArgb = MakeOpaque(SurfaceArgb);
        ForegroundArgb = MakeOpaque(ForegroundArgb);
        AccentArgb = MakeOpaque(AccentArgb);
        SecondaryAccentArgb = MakeOpaque(SecondaryAccentArgb);
        return this;
    }

    private static int MakeOpaque(int argb)
    {
        Color color = Color.FromArgb(argb);
        return Color.FromArgb(255, color.R, color.G, color.B).ToArgb();
    }
}
