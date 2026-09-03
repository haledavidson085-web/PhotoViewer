using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoViewer;

internal static class UpdateService
{
    private const string Repository = "haledavidson085-web/PhotoViewer";
    private const long MaximumExecutableSize = 300L * 1024 * 1024;
    private static readonly HttpClient Client = CreateClient();

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

    public static Task<AvailableUpdate?> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
        CheckForUpdateAsync(CurrentVersion, cancellationToken);

    internal static async Task<AvailableUpdate?> CheckForUpdateAsync(Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"https://api.github.com/repos/{Repository}/releases/latest", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden &&
            response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string>? remaining) &&
            remaining.FirstOrDefault() == "0")
        {
            throw new HttpRequestException("GitHub's API rate limit has been reached. Please try again later.",
                null, response.StatusCode);
        }
        response.EnsureSuccessStatusCode();

        await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(content,
            cancellationToken: cancellationToken);
        if (release is null || !TryParseVersion(release.TagName, out Version? latestVersion))
            throw new InvalidDataException("GitHub returned an invalid release description.");

        if (latestVersion <= currentVersion)
            return null;

        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.Arm64 => "win-arm64",
            _ => throw new NotSupportedException(
                $"Automatic updates are not available for {RuntimeInformation.ProcessArchitecture}.")
        };
        string packageName = $"PhotoViewer-{architecture}-self-contained.zip";
        GitHubAsset? package = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, packageName, StringComparison.OrdinalIgnoreCase));
        GitHubAsset? checksums = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
        if (package is null || checksums is null)
            throw new InvalidDataException($"Release {release.TagName} does not contain the required update files.");

        return new AvailableUpdate(
            latestVersion!,
            release.TagName,
            string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            release.Body ?? string.Empty,
            new Uri(release.HtmlUrl),
            package,
            checksums);
    }

    public static async Task<string> DownloadAndStageAsync(AvailableUpdate update,
        IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        string safeTag = string.Concat(update.TagName.Where(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_'));
        if (safeTag.Length == 0)
            throw new InvalidDataException("The update has an invalid version tag.");

        string stagingDirectory = Path.Combine(GetUpdateRoot(), safeTag);
        Directory.CreateDirectory(stagingDirectory);
        string archivePath = Path.Combine(stagingDirectory, update.Package.Name);
        string checksumPath = Path.Combine(stagingDirectory, update.Checksums.Name);

        await DownloadFileAsync(update.Package.DownloadUrl, archivePath, progress, cancellationToken);
        await DownloadFileAsync(update.Checksums.DownloadUrl, checksumPath, null, cancellationToken);
        await VerifyChecksumAsync(archivePath, checksumPath, update.Package.Name, cancellationToken);

        string stagedExecutable = Path.Combine(stagingDirectory, "PhotoViewer.exe.new");
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry? executable = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullName.Replace('\\', '/'), "PhotoViewer.exe", StringComparison.OrdinalIgnoreCase));
        if (executable is null || executable.Length <= 0 || executable.Length > MaximumExecutableSize)
            throw new InvalidDataException("The update archive does not contain a valid PhotoViewer executable.");

        executable.ExtractToFile(stagedExecutable, overwrite: true);
        return stagedExecutable;
    }

    public static void LaunchUpdater(string stagedExecutable)
    {
        string targetExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("The current executable path is unavailable.");
        if (!string.Equals(Path.GetExtension(targetExecutable), ".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Automatic updates require the packaged PhotoViewer executable.");

        string stagingDirectory = Path.GetDirectoryName(stagedExecutable)!;
        string helperPath = Path.Combine(stagingDirectory, "PhotoViewer.UpdateHelper.exe");
        File.Copy(targetExecutable, helperPath, overwrite: true);

        var startInfo = new ProcessStartInfo(helperPath)
        {
            UseShellExecute = true,
            WorkingDirectory = stagingDirectory
        };
        startInfo.ArgumentList.Add("--apply-update");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.ArgumentList.Add(stagedExecutable);
        startInfo.ArgumentList.Add(targetExecutable);
        startInfo.ArgumentList.Add(stagingDirectory);
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("The update helper could not be started.");
    }

    public static bool TryApplyPendingUpdate(string[] args)
    {
        if (args.Length != 5 || !string.Equals(args[0], "--apply-update", StringComparison.Ordinal))
            return false;

        string? targetToRestart = null;
        try
        {
            if (int.TryParse(args[1], out int parentProcessId))
            {
                try
                {
                    using Process parent = Process.GetProcessById(parentProcessId);
                    parent.WaitForExit(60_000);
                }
                catch (ArgumentException)
                {
                    // The main application has already exited.
                }
            }

            string stagedExecutable = Path.GetFullPath(args[2]);
            string targetExecutable = Path.GetFullPath(args[3]);
            targetToRestart = targetExecutable;
            string stagingDirectory = Path.GetFullPath(args[4]);
            if (!IsInsideUpdateRoot(stagedExecutable) || !IsInsideUpdateRoot(stagingDirectory))
                throw new InvalidOperationException("The staged update path is not trusted.");
            if (!string.Equals(Path.GetExtension(targetExecutable), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The update target is not an executable.");

            string replacementPath = targetExecutable + ".update";
            RetryFileOperation(() => File.Copy(stagedExecutable, replacementPath, overwrite: true));
            RetryFileOperation(() => File.Move(replacementPath, targetExecutable, overwrite: true));

            var startInfo = new ProcessStartInfo(targetExecutable) { UseShellExecute = true };
            startInfo.ArgumentList.Add("--cleanup-update");
            startInfo.ArgumentList.Add(stagingDirectory);
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Photo Viewer could not install the update.\n\n{ex.Message}",
                "Photo Viewer Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (targetToRestart is not null && File.Exists(targetToRestart))
                Process.Start(new ProcessStartInfo(targetToRestart) { UseShellExecute = true });
        }

        return true;
    }

    public static void ScheduleCleanup(string[] args)
    {
        if (args.Length < 2 || !string.Equals(args[0], "--cleanup-update", StringComparison.Ordinal))
            return;

        string directory;
        try
        {
            directory = Path.GetFullPath(args[1]);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return;
        }

        if (!IsInsideUpdateRoot(directory))
            return;

        _ = Task.Run(async () =>
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                await Task.Delay(500);
                try
                {
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, recursive: true);
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
        });
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"PhotoViewer/{CurrentVersion.ToString(3)}");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        string? token = Environment.GetEnvironmentVariable("PHOTOVIEWER_GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        return client;
    }

    private static async Task DownloadFileAsync(Uri source, string destination, IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await Client.GetAsync(source,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        long? totalLength = response.Content.Headers.ContentLength;
        await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None,
            81_920, useAsync: true);
        byte[] buffer = new byte[81_920];
        long received = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            if (totalLength > 0)
                progress?.Report((int)Math.Clamp(received * 100 / totalLength.Value, 0, 100));
        }
    }

    private static async Task VerifyChecksumAsync(string archivePath, string checksumPath, string archiveName,
        CancellationToken cancellationToken)
    {
        string[] lines = await File.ReadAllLinesAsync(checksumPath, cancellationToken);
        string? expectedHex = lines
            .Select(line => line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2 && string.Equals(parts[^1], archiveName, StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts[0])
            .FirstOrDefault();
        if (expectedHex is null || expectedHex.Length != 64)
            throw new InvalidDataException("The release checksum file is invalid.");

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(expectedHex);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("The release checksum is invalid.", ex);
        }

        await using Stream archive = File.OpenRead(archivePath);
        byte[] actual = await SHA256.HashDataAsync(archive, cancellationToken);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            throw new InvalidDataException("The downloaded update failed checksum verification.");
    }

    private static bool TryParseVersion(string tagName, out Version? version) =>
        Version.TryParse(tagName.Trim().TrimStart('v', 'V'), out version);

    private static string GetUpdateRoot() => Path.GetFullPath(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoViewer", "Updates"));

    private static bool IsInsideUpdateRoot(string path)
    {
        string root = GetUpdateRoot().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static void RetryFileOperation(Action operation)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                operation();
                return;
            }
            catch (IOException) when (attempt < 19)
            {
                Thread.Sleep(250);
            }
        }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; init; } = [];
    }
}

internal sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public Uri DownloadUrl { get; init; } = null!;
}

internal sealed record AvailableUpdate(
    Version Version,
    string TagName,
    string Name,
    string Notes,
    Uri PageUrl,
    GitHubAsset Package,
    GitHubAsset Checksums);
