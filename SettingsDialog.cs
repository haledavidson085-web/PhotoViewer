namespace PhotoViewer;

internal sealed class SettingsDialog : Form
{
    private readonly Label fontValue = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly NumericUpDown slideshowInterval = new()
    {
        Minimum = 1,
        Maximum = 60,
        Width = 72,
        TextAlign = HorizontalAlignment.Right
    };
    private readonly Dictionary<string, Button> colorButtons = [];
    private AppSettings workingSettings;
    private Font? previewFont;

    public SettingsDialog(AppSettings settings)
    {
        workingSettings = settings.Clone();
        Text = "Settings — Photo Viewer";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(18);

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Dock = DockStyle.Fill
        };

        root.Controls.Add(new Label
        {
            Text = "Appearance",
            AutoSize = true,
            Font = new Font((SystemFonts.MessageBoxFont ?? Control.DefaultFont).FontFamily, 14f, System.Drawing.FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4)
        });
        root.Controls.Add(new Label
        {
            Text = "Choose the font and colors used throughout the application.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        });
        root.Controls.Add(BuildOptionsPanel());
        root.Controls.Add(BuildColorPanel());
        root.Controls.Add(BuildActionPanel());
        Controls.Add(root);

        UpdateSettingControls();
        ApplyDialogTheme();
    }

    public AppSettings SelectedSettings => workingSettings.Clone().Normalize();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            previewFont?.Dispose();
        base.Dispose(disposing);
    }

    private Control BuildOptionsPanel()
    {
        var panel = CreateTwoColumnPanel();
        panel.Margin = new Padding(0, 0, 0, 14);

        panel.Controls.Add(MakeLabel("Application font"), 0, 0);
        var fontPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Left
        };
        fontPanel.Controls.Add(fontValue);
        var chooseFont = MakeButton("Choose…");
        chooseFont.Click += (_, _) => ChooseFont();
        fontPanel.Controls.Add(chooseFont);
        panel.Controls.Add(fontPanel, 1, 0);

        panel.Controls.Add(MakeLabel("Slideshow interval"), 0, 1);
        var intervalPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Left
        };
        intervalPanel.Controls.Add(slideshowInterval);
        slideshowInterval.ValueChanged += (_, _) =>
            workingSettings.SlideshowIntervalSeconds = (int)slideshowInterval.Value;
        intervalPanel.Controls.Add(new Label { Text = "seconds", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 6, 0, 0) });
        panel.Controls.Add(intervalPanel, 1, 1);
        return panel;
    }

    private Control BuildColorPanel()
    {
        var group = new GroupBox
        {
            Text = "Colors",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 16)
        };
        var panel = CreateTwoColumnPanel();
        panel.Margin = Padding.Empty;

        AddColorRow(panel, 0, "Window background", nameof(AppSettings.WindowBackgroundArgb));
        AddColorRow(panel, 1, "Photo canvas", nameof(AppSettings.CanvasBackgroundArgb));
        AddColorRow(panel, 2, "Menus and toolbars", nameof(AppSettings.SurfaceArgb));
        AddColorRow(panel, 3, "Text", nameof(AppSettings.ForegroundArgb));
        AddColorRow(panel, 4, "Primary accent", nameof(AppSettings.AccentArgb));
        AddColorRow(panel, 5, "Secondary accent", nameof(AppSettings.SecondaryAccentArgb));
        group.Controls.Add(panel);
        return group;
    }

    private Control BuildActionPanel()
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var save = MakeButton("Save");
        save.DialogResult = DialogResult.OK;
        var cancel = MakeButton("Cancel");
        cancel.DialogResult = DialogResult.Cancel;
        var reset = MakeButton("Reset defaults");
        reset.Click += (_, _) =>
        {
            workingSettings = AppSettings.Default;
            UpdateSettingControls();
            ApplyDialogTheme();
        };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actions.Controls.Add(reset);
        AcceptButton = save;
        CancelButton = cancel;
        return actions;
    }

    private void AddColorRow(TableLayoutPanel panel, int row, string label, string propertyName)
    {
        panel.Controls.Add(MakeLabel(label), 0, row);
        var button = MakeButton(string.Empty);
        button.Width = 148;
        button.Tag = propertyName;
        button.Click += (_, _) => ChooseColor(propertyName);
        colorButtons[propertyName] = button;
        panel.Controls.Add(button, 1, row);
    }

    private void ChooseFont()
    {
        using Font current = workingSettings.CreateFont();
        using var dialog = new FontDialog
        {
            Font = current,
            FontMustExist = true,
            ShowColor = false,
            ShowEffects = true,
            MinSize = 8,
            MaxSize = 20
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        workingSettings.FontFamily = dialog.Font.FontFamily.Name;
        workingSettings.FontSize = dialog.Font.SizeInPoints;
        workingSettings.FontStyle = (int)dialog.Font.Style;
        UpdateSettingControls();
        ApplyDialogTheme();
    }

    private void ChooseColor(string propertyName)
    {
        var property = typeof(AppSettings).GetProperty(propertyName)!;
        using var dialog = new ColorDialog
        {
            Color = Color.FromArgb((int)property.GetValue(workingSettings)!),
            FullOpen = true,
            AnyColor = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        property.SetValue(workingSettings, dialog.Color.ToArgb());
        UpdateColorButtons();
        ApplyDialogTheme();
    }

    private void UpdateSettingControls()
    {
        using Font font = workingSettings.CreateFont();
        fontValue.Text = $"{font.Name}, {font.SizeInPoints:0.#} pt{StyleSuffix(font.Style)}";
        slideshowInterval.Value = Math.Clamp(workingSettings.SlideshowIntervalSeconds, 1, 60);
        UpdateColorButtons();
    }

    private void UpdateColorButtons()
    {
        foreach ((string propertyName, Button button) in colorButtons)
        {
            int argb = (int)typeof(AppSettings).GetProperty(propertyName)!.GetValue(workingSettings)!;
            Color color = Color.FromArgb(argb);
            button.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            button.BackColor = color;
            button.ForeColor = IsLight(color) ? Color.Black : Color.White;
            button.FlatAppearance.BorderColor = IsLight(color) ? Color.FromArgb(80, 0, 0, 0) : Color.FromArgb(110, 255, 255, 255);
        }
    }

    private void ApplyDialogTheme()
    {
        previewFont?.Dispose();
        previewFont = workingSettings.CreateFont();
        Font = previewFont;
        BackColor = Color.FromArgb(workingSettings.WindowBackgroundArgb);
        ForeColor = Color.FromArgb(workingSettings.ForegroundArgb);
        ApplyThemeToChildren(this);
        UpdateColorButtons();
    }

    private void ApplyThemeToChildren(Control parent)
    {
        Color surface = Color.FromArgb(workingSettings.SurfaceArgb);
        foreach (Control control in parent.Controls)
        {
            control.ForeColor = ForeColor;
            if (control is GroupBox or TableLayoutPanel or FlowLayoutPanel)
                control.BackColor = BackColor;
            else if (control is Button button && !colorButtons.ContainsValue(button))
            {
                button.BackColor = surface;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = AppTheme.Blend(surface, ForeColor, 0.22f);
            }
            else if (control is NumericUpDown)
                control.BackColor = surface;

            ApplyThemeToChildren(control);
        }
    }

    private static TableLayoutPanel CreateTwoColumnPanel()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Fill
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return panel;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 8, 12, 8)
    };

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(82, 30),
        Margin = new Padding(4)
    };

    private static bool IsLight(Color color) =>
        (color.R * 299 + color.G * 587 + color.B * 114) / 1000 >= 150;

    private static string StyleSuffix(System.Drawing.FontStyle style) =>
        style == System.Drawing.FontStyle.Regular ? string.Empty : $", {style}";
}
