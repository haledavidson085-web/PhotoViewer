using System.Diagnostics;

namespace PhotoViewer;

internal sealed class MainForm : Form
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".ico", ".jpeg", ".jpg", ".png", ".tif", ".tiff"
    };

    private readonly ImageCanvas canvas = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel fileStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel dimensionsStatus = new();
    private readonly ToolStripStatusLabel positionStatus = new();
    private readonly ToolStripStatusLabel zoomStatus = new();
    private readonly ToolStripButton previousButton;
    private readonly ToolStripButton nextButton;
    private readonly ToolStripButton slideshowButton;
    private readonly System.Windows.Forms.Timer slideshowTimer = new() { Interval = 3000 };
    private readonly List<string> files = [];
    private int currentIndex = -1;
    private Image? currentImage;
    private bool fullScreen;
    private FormBorderStyle savedBorderStyle;
    private FormWindowState savedWindowState;

    public MainForm(string? initialPath)
    {
        Text = "Photo Viewer";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 480);
        ClientSize = new Size(1100, 720);
        KeyPreview = true;
        AllowDrop = true;
        BackColor = canvas.BackColor;

        var menu = BuildMenu();
        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(6, 3, 6, 3),
            ImageScalingSize = new Size(20, 20)
        };

        toolbar.Items.Add(MakeButton("Open", (_, _) => OpenImage(), "Open an image (Ctrl+O)"));
        toolbar.Items.Add(MakeButton("Folder", (_, _) => OpenFolder(), "Open a folder (Ctrl+Shift+O)"));
        toolbar.Items.Add(new ToolStripSeparator());
        previousButton = MakeButton("Previous", (_, _) => Navigate(-1), "Previous photo (Left)");
        nextButton = MakeButton("Next", (_, _) => Navigate(1), "Next photo (Right)");
        toolbar.Items.Add(previousButton);
        toolbar.Items.Add(nextButton);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeButton("Zoom -", (_, _) => canvas.ZoomOut(), "Zoom out (-)"));
        toolbar.Items.Add(MakeButton("Zoom +", (_, _) => canvas.ZoomIn(), "Zoom in (+)"));
        toolbar.Items.Add(MakeButton("Fit", (_, _) => canvas.Fit(), "Fit to window (F)"));
        toolbar.Items.Add(MakeButton("100%", (_, _) => canvas.ActualSize(), "Actual size (1)"));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(MakeButton("Rotate left", (_, _) => Rotate(RotateFlipType.Rotate270FlipNone), "Rotate left"));
        toolbar.Items.Add(MakeButton("Rotate right", (_, _) => Rotate(RotateFlipType.Rotate90FlipNone), "Rotate right"));
        toolbar.Items.Add(new ToolStripSeparator());
        slideshowButton = MakeButton("Slideshow", (_, _) => ToggleSlideshow(), "Start slideshow (F5)");
        slideshowButton.CheckOnClick = true;
        toolbar.Items.Add(slideshowButton);

        var status = new StatusStrip();
        status.Items.AddRange([fileStatus, dimensionsStatus, positionStatus, zoomStatus]);

        Controls.Add(canvas);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(status);
        MainMenuStrip = menu;

        canvas.ViewChanged += (_, _) => UpdateStatus();
        slideshowTimer.Tick += (_, _) => Navigate(1);
        DragEnter += HandleDragEnter;
        DragDrop += HandleDragDrop;
        FormClosed += (_, _) => currentImage?.Dispose();
        KeyDown += HandleKeyDown;
        Shown += (_, _) => OpenInitialPath(initialPath);

        UpdateNavigationState();
        UpdateStatus();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(new ToolStripMenuItem("&Open image...", null, (_, _) => OpenImage(), Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripMenuItem("Open &folder...", null, (_, _) => OpenFolder(), Keys.Control | Keys.Shift | Keys.O));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("Open file &location", null, (_, _) => OpenFileLocation()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close(), Keys.Alt | Keys.F4));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(new ToolStripMenuItem("Zoom &in", null, (_, _) => canvas.ZoomIn(), Keys.Control | Keys.Oemplus));
        view.DropDownItems.Add(new ToolStripMenuItem("Zoom &out", null, (_, _) => canvas.ZoomOut(), Keys.Control | Keys.OemMinus));
        view.DropDownItems.Add(new ToolStripMenuItem("&Fit to window", null, (_, _) => canvas.Fit())
        {
            ShortcutKeyDisplayString = "F"
        });
        view.DropDownItems.Add(new ToolStripMenuItem("&Actual size", null, (_, _) => canvas.ActualSize())
        {
            ShortcutKeyDisplayString = "1"
        });
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(new ToolStripMenuItem("&Full screen", null, (_, _) => ToggleFullScreen(), Keys.F11));

        var image = new ToolStripMenuItem("&Image");
        image.DropDownItems.Add(new ToolStripMenuItem("&Previous", null, (_, _) => Navigate(-1))
        {
            ShortcutKeyDisplayString = "Left"
        });
        image.DropDownItems.Add(new ToolStripMenuItem("&Next", null, (_, _) => Navigate(1))
        {
            ShortcutKeyDisplayString = "Right"
        });
        image.DropDownItems.Add(new ToolStripSeparator());
        image.DropDownItems.Add(new ToolStripMenuItem("Rotate &left", null, (_, _) => Rotate(RotateFlipType.Rotate270FlipNone), Keys.Control | Keys.L));
        image.DropDownItems.Add(new ToolStripMenuItem("Rotate &right", null, (_, _) => Rotate(RotateFlipType.Rotate90FlipNone), Keys.Control | Keys.R));
        image.DropDownItems.Add(new ToolStripSeparator());
        image.DropDownItems.Add(new ToolStripMenuItem("&Slideshow", null, (_, _) => ToggleSlideshow(), Keys.F5));

        menu.Items.AddRange([file, view, image]);
        return menu;
    }

    private static ToolStripButton MakeButton(string text, EventHandler onClick, string tooltip)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = tooltip };
        button.Click += onClick;
        return button;
    }

    private void OpenImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open a photo",
            Filter = "Images|*.bmp;*.gif;*.ico;*.jpeg;*.jpg;*.png;*.tif;*.tiff|All files|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadFolderAndSelect(dialog.FileName);
    }

    private void OpenFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a folder containing photos",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        LoadFolder(dialog.SelectedPath);
        if (files.Count > 0)
            ShowAt(0);
        else
            MessageBox.Show(this, "No supported images were found in that folder.", "Photo Viewer",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenInitialPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (Directory.Exists(path))
        {
            LoadFolder(path);
            if (files.Count > 0)
                ShowAt(0);
        }
        else if (File.Exists(path) && IsSupported(path))
        {
            LoadFolderAndSelect(path);
        }
    }

    private void LoadFolderAndSelect(string path)
    {
        string fullPath = Path.GetFullPath(path);
        LoadFolder(Path.GetDirectoryName(fullPath)!);
        int index = files.FindIndex(file => string.Equals(file, fullPath, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            ShowAt(index);
    }

    private void LoadFolder(string folder)
    {
        files.Clear();
        try
        {
            files.AddRange(Directory.EnumerateFiles(folder)
                .Where(IsSupported)
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The folder could not be read.\n\n{ex.Message}", "Photo Viewer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        UpdateNavigationState();
    }

    private void ShowAt(int index)
    {
        if (files.Count == 0)
            return;

        index = (index % files.Count + files.Count) % files.Count;
        string path = files[index];

        try
        {
            Image loaded = LoadUnlocked(path);
            Image? oldImage = currentImage;
            currentImage = loaded;
            currentIndex = index;
            canvas.Image = loaded;
            oldImage?.Dispose();
            Text = $"{Path.GetFileName(path)} — Photo Viewer";
            UpdateNavigationState();
            UpdateStatus();
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or OutOfMemoryException)
        {
            MessageBox.Show(this, $"The image could not be opened.\n\n{ex.Message}", "Photo Viewer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Image LoadUnlocked(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes);
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private void Navigate(int offset)
    {
        if (files.Count > 0)
            ShowAt(currentIndex < 0 ? 0 : currentIndex + offset);
    }

    private void Rotate(RotateFlipType rotation)
    {
        if (currentImage is null)
            return;

        currentImage.RotateFlip(rotation);
        canvas.Fit();
        canvas.Invalidate();
        UpdateStatus();
    }

    private void ToggleSlideshow()
    {
        if (files.Count < 2)
            return;

        slideshowTimer.Enabled = !slideshowTimer.Enabled;
        slideshowButton.Checked = slideshowTimer.Enabled;
        slideshowButton.Text = slideshowTimer.Enabled ? "Stop slideshow" : "Slideshow";
    }

    private void ToggleFullScreen()
    {
        if (!fullScreen)
        {
            savedBorderStyle = FormBorderStyle;
            savedWindowState = WindowState;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            fullScreen = true;
        }
        else
        {
            FormBorderStyle = savedBorderStyle;
            WindowState = savedWindowState;
            fullScreen = false;
        }
    }

    private void OpenFileLocation()
    {
        if (currentIndex < 0)
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{files[currentIndex]}\"")
        {
            UseShellExecute = true
        });
    }

    private void UpdateNavigationState()
    {
        bool canNavigate = files.Count > 1;
        previousButton.Enabled = canNavigate;
        nextButton.Enabled = canNavigate;
        slideshowButton.Enabled = canNavigate;
        if (!canNavigate && slideshowTimer.Enabled)
            ToggleSlideshow();
    }

    private void UpdateStatus()
    {
        if (currentImage is null || currentIndex < 0)
        {
            fileStatus.Text = "No photo open";
            dimensionsStatus.Text = string.Empty;
            positionStatus.Text = string.Empty;
            zoomStatus.Text = string.Empty;
            return;
        }

        fileStatus.Text = Path.GetFileName(files[currentIndex]);
        dimensionsStatus.Text = $"{currentImage.Width:N0} × {currentImage.Height:N0}";
        positionStatus.Text = $"{currentIndex + 1:N0} of {files.Count:N0}";
        zoomStatus.Text = $"{canvas.EffectiveZoom:P0}";
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        bool handled = true;

        switch (e.KeyCode)
        {
            case Keys.Escape when e.Modifiers == Keys.None && fullScreen:
                ToggleFullScreen();
                break;
            case Keys.F when e.Modifiers == Keys.None:
                canvas.Fit();
                break;
            case Keys.D1 or Keys.NumPad1 when e.Modifiers == Keys.None:
                canvas.ActualSize();
                break;
            case Keys.Left when e.Modifiers == Keys.None:
                Navigate(-1);
                break;
            case Keys.Right when e.Modifiers == Keys.None:
                Navigate(1);
                break;
            case Keys.Add or Keys.Oemplus when !e.Control && !e.Alt:
                canvas.ZoomIn();
                break;
            case Keys.Subtract or Keys.OemMinus when !e.Control && !e.Alt:
                canvas.ZoomOut();
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void HandleDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
            return;

        string path = paths[0];
        if (Directory.Exists(path))
        {
            LoadFolder(path);
            if (files.Count > 0)
                ShowAt(0);
        }
        else if (File.Exists(path) && IsSupported(path))
        {
            LoadFolderAndSelect(path);
        }
    }

    private static bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path));
}
