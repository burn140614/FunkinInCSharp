using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Funkin.Util;

namespace Funkin.Assets;

/// <summary>Where to fetch the game's assets from.</summary>
public sealed class AssetSource
{
    public string Owner { get; }
    public string Repo { get; }
    public string Branch { get; }

    private readonly string? _urlOverride;

    public AssetSource(string owner, string repo, string branch = "main", string? urlOverride = null)
    {
        Owner = owner;
        Repo = repo;
        Branch = branch;
        _urlOverride = urlOverride;
    }

    public static AssetSource Default { get; } = new("FunkinCrew", "Funkin.assets");

    /// <summary>Direct download URL of the repository archive (no git required).</summary>
    public string ArchiveUrl => _urlOverride ?? $"https://codeload.github.com/{Owner}/{Repo}/zip/refs/heads/{Branch}";

    public string DisplayName => _urlOverride is null ? $"{Owner}/{Repo}" : _urlOverride;

    public override string ToString() => $"{DisplayName}@{Branch}";
}

public enum AssetInstallPhase
{
    Preparing,
    Downloading,
    Extracting,
    Done,
    Failed,
    Cancelled,
}

public enum AssetInstallStatus
{
    Success,
    Cancelled,
    NoSpace,
    NetworkError,
    ArchiveError,
    AlreadyPresent,
}

/// <summary>A snapshot of the installer's progress, polled by the UI.</summary>
public sealed class AssetDownloadProgress
{
    public AssetInstallPhase Phase { get; init; }
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }
    public long BytesExtracted { get; init; }
    public long TotalBytesToExtract { get; init; }
    public int FilesExtracted { get; init; }
    public int FilesTotal { get; init; }
    public double BytesPerSecond { get; init; }
    public string Message { get; init; } = string.Empty;

    public double Fraction => Phase switch
    {
        AssetInstallPhase.Downloading => TotalBytes is > 0 ? Math.Clamp((double)BytesReceived / TotalBytes.Value, 0, 1) : 0,
        AssetInstallPhase.Extracting => TotalBytesToExtract > 0 ? Math.Clamp((double)BytesExtracted / TotalBytesToExtract, 0, 1) : FractionByFiles,
        AssetInstallPhase.Done => 1,
        _ => 0,
    };

    private double FractionByFiles => FilesTotal > 0 ? Math.Clamp((double)FilesExtracted / FilesTotal, 0, 1) : 0;

    /// <summary>Estimated time remaining, or null when it cannot be estimated yet.</summary>
    public TimeSpan? Eta
    {
        get
        {
            if (Phase != AssetInstallPhase.Downloading || BytesPerSecond <= 1 || TotalBytes is not > 0) return null;
            double remaining = Math.Max(0, TotalBytes.Value - BytesReceived);
            return TimeSpan.FromSeconds(remaining / BytesPerSecond);
        }
    }
}

public sealed class AssetInstallResult
{
    public required AssetInstallStatus Status { get; init; }
    public string? Message { get; init; }
    public long BytesDownloaded { get; init; }
    public int FilesExtracted { get; init; }

    public bool Success => Status is AssetInstallStatus.Success or AssetInstallStatus.AlreadyPresent;
}

/// <summary>
/// Downloads the game's asset repository and arranges it into the runtime `assets/` layout.
///
/// No git install is required: the repository archive (zipball) is streamed from GitHub, with
/// byte-range resume so an interrupted 3.7 GB download does not have to start over.
/// </summary>
public sealed class AssetDownloader : IDisposable
{
    private readonly AssetSource _source;
    private readonly string _gameRoot;
    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public AssetDownloader(AssetSource source, string gameRoot, HttpClient? client = null)
    {
        _source = source;
        _gameRoot = gameRoot;
        _client = client ?? CreateClient();
        _ownsClient = client is null;
    }

    public string AssetsRoot => Path.Combine(_gameRoot, "assets");

    /// <summary>Where the in-progress archive is kept so a cancelled download can resume.</summary>
    public string WorkingDirectory => Path.Combine(_gameRoot, ".asset-download");

    public string ArchivePath => Path.Combine(WorkingDirectory, "assets.zip");

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None,
            AllowAutoRedirect = true,
        })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("FunkinCSharp/0.10 (asset installer)");
        return client;
    }

    /// <summary>Downloads and installs the assets. Safe to cancel; partial downloads resume.</summary>
    public async Task<AssetInstallResult> InstallAsync(IProgress<AssetDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (AssetLayout.IsAssetsPresent(_gameRoot))
            {
                return new AssetInstallResult { Status = AssetInstallStatus.AlreadyPresent };
            }

            Directory.CreateDirectory(WorkingDirectory);
            Logger.Info($"Installing assets from {_source.ArchiveUrl} into {AssetsRoot}");

            progress?.Report(new AssetDownloadProgress
            {
                Phase = AssetInstallPhase.Preparing,
                Message = $"Contacting {_source.DisplayName}...",
            });

            var download = await DownloadArchiveAsync(progress, cancellationToken).ConfigureAwait(false);
            if (!download.Success) return download.Result!;

            var extract = ExtractArchive(progress, cancellationToken);
            if (extract.Success)
            {
                CleanupArchive();
            }
            else if (extract.Status == AssetInstallStatus.ArchiveError)
            {
                // The archive is unusable (truncated or not a zip at all); drop it so the next
                // attempt starts from a clean download instead of failing forever on the same file.
                Logger.Warn($"Discarding unusable asset archive: {extract.Message}");
                CleanupArchive(force: true);
            }

            return extract;
        }
        catch (OperationCanceledException)
        {
            return new AssetInstallResult { Status = AssetInstallStatus.Cancelled, Message = "Download cancelled." };
        }
        catch (Exception ex)
        {
            return new AssetInstallResult { Status = AssetInstallStatus.NetworkError, Message = ex.Message };
        }
        finally
        {
            // The client stays alive so a failed or cancelled install can be retried/resumed.
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }

    private sealed record DownloadOutcome(bool Success, AssetInstallResult? Result, long Bytes);

    private async Task<DownloadOutcome> DownloadArchiveAsync(IProgress<AssetDownloadProgress>? progress, CancellationToken ct)
    {
        long existing = File.Exists(ArchivePath) ? new FileInfo(ArchivePath).Length : 0;

        using var request = new HttpRequestMessage(HttpMethod.Get, _source.ArchiveUrl);
        if (existing > 0) request.Headers.Range = new RangeHeaderValue(existing, null);

        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (existing > 0 && response.StatusCode == HttpStatusCode.OK)
        {
            // The server ignored the range request, so the partial file is useless.
            existing = 0;
            File.Delete(ArchivePath);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new DownloadOutcome(false, new AssetInstallResult
            {
                Status = AssetInstallStatus.NetworkError,
                Message = $"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase} for {_source.ArchiveUrl}",
            }, 0);
        }

        if (existing > 0 && response.StatusCode == HttpStatusCode.PartialContent)
        {
            Logger.Info($"Resuming asset download at {FormatSize(existing)}.");
            progress?.Report(new AssetDownloadProgress
            {
                Phase = AssetInstallPhase.Downloading,
                BytesReceived = existing,
                TotalBytes = response.Content.Headers.ContentLength + existing,
                Message = $"Resuming download from {FormatSize(existing)}...",
            });
        }

        long? total = response.Content.Headers.ContentLength is { } length
            ? length + existing
            : null;

        if (total is > 0)
        {
            long available = AssetLayout.GetAvailableDiskSpace(_gameRoot);
            if (available < total.Value + (total.Value / 4))
            {
                return new DownloadOutcome(false, new AssetInstallResult
                {
                    Status = AssetInstallStatus.NoSpace,
                    Message = $"Not enough disk space: need about {FormatSize((long)(total.Value * 1.25))} free, have {FormatSize(available)}.",
                }, 0);
            }
        }

        await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var destination = new FileStream(ArchivePath, existing > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true);

        var buffer = new byte[1 << 20];
        long received = existing;
        long receivedThisRun = 0;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            int read = await source.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read <= 0) break;

            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            received += read;
            receivedThisRun += read;

            if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(100))
            {
                lastReport = stopwatch.Elapsed;
                progress?.Report(new AssetDownloadProgress
                {
                    Phase = AssetInstallPhase.Downloading,
                    BytesReceived = received,
                    TotalBytes = total,
                    BytesPerSecond = receivedThisRun / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds),
                    Message = $"Downloading assets from {_source.DisplayName}...",
                });
            }
        }

        await destination.FlushAsync(ct).ConfigureAwait(false);

        if (total is > 0 && received < total.Value)
        {
            return new DownloadOutcome(false, new AssetInstallResult
            {
                Status = AssetInstallStatus.NetworkError,
                Message = $"Download ended early ({FormatSize(received)} of {FormatSize(total.Value)}); it will resume next time.",
            }, received);
        }

        progress?.Report(new AssetDownloadProgress
        {
            Phase = AssetInstallPhase.Downloading,
            BytesReceived = received,
            TotalBytes = total ?? received,
            BytesPerSecond = receivedThisRun / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds),
            Message = "Download complete, unpacking...",
        });

        return new DownloadOutcome(true, null, received);
    }

    private AssetInstallResult ExtractArchive(IProgress<AssetDownloadProgress>? progress, CancellationToken ct)
    {
        ZipArchive archive;
        try
        {
            archive = ZipFile.OpenRead(ArchivePath);
        }
        catch (Exception ex)
        {
            return new AssetInstallResult { Status = AssetInstallStatus.ArchiveError, Message = $"Could not read the downloaded archive: {ex.Message}" };
        }

        using (archive)
        {
            var entries = archive.Entries
                .Select(entry => (Entry: entry, Target: AssetLayout.MapArchiveEntry(entry.FullName)))
                .Where(pair => pair.Target is not null)
                .ToList();

            if (entries.Count == 0)
            {
                return new AssetInstallResult { Status = AssetInstallStatus.ArchiveError, Message = "The archive did not contain any game assets." };
            }

            long totalBytes = entries.Sum(pair => Math.Max(0, pair.Entry.Length));
            long extractedBytes = 0;
            int extractedFiles = 0;
            var stopwatch = Stopwatch.StartNew();
            var lastReport = TimeSpan.Zero;

            string assetsRoot = Path.GetFullPath(AssetsRoot);
            Directory.CreateDirectory(assetsRoot);

            progress?.Report(new AssetDownloadProgress
            {
                Phase = AssetInstallPhase.Extracting,
                BytesExtracted = 0,
                TotalBytesToExtract = totalBytes,
                FilesExtracted = 0,
                FilesTotal = entries.Count,
                Message = $"Installing assets... (0/{entries.Count} files)",
            });

            foreach (var (entry, target) in entries)
            {
                ct.ThrowIfCancellationRequested();

                string destination = Path.GetFullPath(Path.Combine(assetsRoot, target!));

                // Defend against archive entries trying to escape the assets folder.
                if (!destination.StartsWith(assetsRoot, StringComparison.Ordinal))
                {
                    continue;
                }

                string? directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                try
                {
                    entry.ExtractToFile(destination, overwrite: true);
                }
                catch (IOException)
                {
                    // A locked file (usually a texture in use) should not abort the whole install.
                }

                extractedFiles++;
                extractedBytes += Math.Max(0, entry.Length);

                if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(100))
                {
                    lastReport = stopwatch.Elapsed;
                    progress?.Report(new AssetDownloadProgress
                    {
                        Phase = AssetInstallPhase.Extracting,
                        BytesExtracted = extractedBytes,
                        TotalBytesToExtract = totalBytes,
                        FilesExtracted = extractedFiles,
                        FilesTotal = entries.Count,
                        Message = $"Installing assets... ({extractedFiles}/{entries.Count} files)",
                    });
                }
            }

            progress?.Report(new AssetDownloadProgress
            {
                Phase = AssetInstallPhase.Done,
                BytesExtracted = extractedBytes,
                TotalBytesToExtract = totalBytes,
                FilesExtracted = extractedFiles,
                FilesTotal = entries.Count,
                Message = $"Installed {extractedFiles} asset files.",
            });

            Logger.Info($"Extracted {extractedFiles} asset files ({FormatSize(extractedBytes)}).");

            return new AssetInstallResult
            {
                Status = AssetInstallStatus.Success,
                BytesDownloaded = new FileInfo(ArchivePath).Length,
                FilesExtracted = extractedFiles,
                Message = $"Installed {extractedFiles} files.",
            };
        }
    }

    private void CleanupArchive(bool force = false)
    {
        try
        {
            if (File.Exists(ArchivePath)) File.Delete(ArchivePath);
            if (Directory.Exists(WorkingDirectory) && Directory.GetFileSystemEntries(WorkingDirectory).Length == 0)
            {
                Directory.Delete(WorkingDirectory);
            }
        }
        catch
        {
            // Leaving the archive behind only costs disk space, and enables a resume.
        }
    }

    public static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}
