# Photo Viewer

A lightweight Windows photo viewer built with WinForms and .NET 10. It uses only the Windows Desktop framework and built-in .NET namespaces—no third-party packages.

## Features

- Open an image, a folder, a dropped file, or a path passed on the command line
- Browse all supported images in a folder with wraparound navigation
- Fit-to-window and actual-size views
- Mouse-wheel zoom and click-drag panning
- Rotate images left or right without changing the source file
- Full-screen mode and an automatic slideshow
- A dark interface with a custom application icon and high-contrast controls
- An icon-and-text toolbar that keeps common actions easy to recognize
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

## Download packages

Each GitHub Actions run produces Windows x64 and ARM64 packages in two forms:

- **Framework-dependent:** smaller download; requires the .NET 10 Desktop Runtime.
- **Self-contained:** larger download; includes the .NET runtime and does not require a separate installation.

Both package types use a single-file executable and are available from the workflow run's **Artifacts** section for 14 days.

To publish a permanent GitHub Release containing all four packages and a `SHA256SUMS.txt` file, push a version tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

GitHub automatically generates the release notes from merged pull requests. The repository's `.github/release.yml` groups entries into breaking changes, features, fixes, documentation, dependencies, maintenance, and other changes. Apply the `skip-changelog` or `ignore-for-release` label to omit a pull request from the notes.

## Repository automation

- Structured forms label bug reports, feature requests, and questions for triage.
- Pull requests receive a checklist and labels based on the files changed.
- Inactive issues and pull requests receive a warning before automatic closure.
- Dependabot checks GitHub Actions for updates every week.
- Markdown under `wiki/` is automatically published when GitHub Wiki support is available; otherwise the workflow preserves the version-controlled pages without failing CI.
