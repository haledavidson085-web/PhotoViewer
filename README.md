# Photo Viewer

A lightweight Windows photo viewer built with WinForms and .NET 10. It uses only the Windows Desktop framework and built-in .NET namespaces—no third-party packages.

## Features

- Open an image, a folder, a dropped file, or a path passed on the command line
- Browse all supported images in a folder with wraparound navigation
- Fit-to-window and actual-size views
- Mouse-wheel zoom and click-drag panning
- Rotate images left or right without changing the source file
- Full-screen mode and an automatic slideshow
- Status details for filename, dimensions, folder position, and zoom
- Loads images without keeping source files locked

Supported formats: BMP, GIF, ICO, JPEG, PNG, and TIFF.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build from source

## Build and run

```powershell
dotnet build
dotnet run
```

You can also open an image or folder at launch:

```powershell
dotnet run -- "C:\Pictures\photo.jpg"
```

## Keyboard shortcuts

| Action | Shortcut |
| --- | --- |
| Open image | `Ctrl+O` |
| Open folder | `Ctrl+Shift+O` |
| Previous / next image | `Left` / `Right` |
| Zoom in / out | `+` / `-` |
| Fit to window | `F` |
| Actual size | `1` |
| Full screen | `F11` |
| Leave full screen | `Esc` |
| Start / stop slideshow | `F5` |
| Rotate left / right | `Ctrl+L` / `Ctrl+R` |

Mouse controls: use the wheel to zoom, drag to pan, and double-click to switch between fit and actual size.
