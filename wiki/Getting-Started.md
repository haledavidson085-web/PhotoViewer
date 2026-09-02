# Getting started

## Install a release

Download the package that matches your Windows architecture from a GitHub Actions run or a tagged GitHub Release.

- **Self-contained:** includes the .NET runtime and runs without a separate runtime installation.
- **Framework-dependent:** smaller, but requires the .NET 10 Desktop Runtime.

Extract the ZIP and run `PhotoViewer.exe`.

## Build from source

Install the .NET 10 SDK, clone the repository, and run:

```powershell
dotnet build --configuration Release
dotnet run
```

Pass an image or folder path to open it immediately:

```powershell
dotnet run -- "C:\Pictures"
```

Supported formats are BMP, GIF, ICO, JPEG, PNG, and TIFF.
