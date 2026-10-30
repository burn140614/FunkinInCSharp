using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Funkin.Assets;
using Xunit;

namespace Funkin.Tests;

public class AssetLayoutTests
{
    [Theory]
    [InlineData("Funkin.assets-main/preload/images/NOTE_assets.png", "images/NOTE_assets.png")]
    [InlineData("Funkin.assets-main/preload/data/songs/bopeebo/bopeebo-chart.json", "data/songs/bopeebo/bopeebo-chart.json")]
    [InlineData("Funkin.assets-main/preload/music/freakyMenu.ogg", "music/freakyMenu.ogg")]
    [InlineData("Funkin.assets-main/songs/bopeebo/Inst.ogg", "songs/bopeebo/Inst.ogg")]
    [InlineData("Funkin.assets-main/shared/images/stage/back.png", "shared/images/stage/back.png")]
    [InlineData("Funkin.assets-main/week1/images/week1.png", "week1/images/week1.png")]
    [InlineData("Funkin.assets-main/fonts/vcr.ttf", "fonts/vcr.ttf")]
    [InlineData("Funkin.assets-main/videos/cutscene.mp4", "videos/cutscene.mp4")]
    [InlineData("preload/images/NOTE_assets.png", "images/NOTE_assets.png")]
    public void MapsRepositoryEntriesOntoTheRuntimeTree(string entry, string expected)
    {
        Assert.Equal(expected, AssetLayout.MapArchiveEntry(entry));
    }

    [Theory]
    [InlineData("Funkin.assets-main/.github/workflows/build.yml")]
    [InlineData("Funkin.assets-main/README.md")]
    [InlineData("Funkin.assets-main/LICENSE.md")]
    [InlineData("Funkin.assets-main/.gitignore")]
    [InlineData("Funkin.assets-main/preload/")]
    [InlineData("")]
    public void SkipsNonGameFiles(string entry)
    {
        Assert.Null(AssetLayout.MapArchiveEntry(entry));
    }

    [Fact]
    public void DetectsAssembledAndUnassembledInstalls()
    {
        string root = CreateTempRoot();
        try
        {
            Assert.False(AssetLayout.IsAssetsPresent(root));
            Assert.False(AssetLayout.IsUnassembledCheckout(root));

            // Assembled layout: assets/data/songs/...
            Directory.CreateDirectory(Path.Combine(root, "assets", "data", "songs", "bopeebo"));
            Assert.True(AssetLayout.IsAssetsPresent(root));
            Assert.False(AssetLayout.IsUnassembledCheckout(root));
        }
        finally
        {
            Directory.Delete(root, true);
        }

        string checkout = CreateTempRoot();
        try
        {
            // Raw clone of the repository: assets/preload/data/songs/...
            Directory.CreateDirectory(Path.Combine(checkout, "assets", "preload", "data", "songs", "bopeebo"));
            Assert.True(AssetLayout.IsAssetsPresent(checkout));
            Assert.True(AssetLayout.IsUnassembledCheckout(checkout));
        }
        finally
        {
            Directory.Delete(checkout, true);
        }
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "funkin-assets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

public class AssetDownloaderTests
{
    [Fact]
    public async Task DownloadsAndArrangesTheAssetsTree()
    {
        byte[] zip = BuildArchive(new Dictionary<string, string>
        {
            ["preload/images/NOTE_assets.png"] = "png-bytes",
            ["preload/images/NOTE_assets.xml"] = "<xml/>",
            ["preload/data/songs/bopeebo/bopeebo-metadata.json"] = "{}",
            ["songs/bopeebo/Inst.ogg"] = "ogg-bytes",
            ["shared/images/stage/back.png"] = "png-bytes",
            ["week1/images/week1.png"] = "png-bytes",
            ["fonts/vcr.ttf"] = "ttf",
            [".github/workflows/build.yml"] = "nope",
            ["README.md"] = "readme",
        });

        using var server = new ZipServer(zip);
        string root = CreateTempRoot();

        try
        {
            var reported = new List<AssetDownloadProgress>();
            var progress = new Progress<AssetDownloadProgress>(reported.Add);

            var downloader = new AssetDownloader(new AssetSource("test", "assets", "main", server.Url), root);
            var result = await downloader.InstallAsync(progress);

            Assert.Equal(AssetInstallStatus.Success, result.Status);
            Assert.Equal(7, result.FilesExtracted);

            // preload/ is flattened into assets/, everything else keeps its folder.
            Assert.True(File.Exists(Path.Combine(root, "assets", "images", "NOTE_assets.png")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "data", "songs", "bopeebo", "bopeebo-metadata.json")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "songs", "bopeebo", "Inst.ogg")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "shared", "images", "stage", "back.png")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "week1", "images", "week1.png")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "fonts", "vcr.ttf")));

            Assert.False(File.Exists(Path.Combine(root, "assets", "README.md")));
            Assert.False(Directory.Exists(Path.Combine(root, "assets", ".github")));

            Assert.True(AssetLayout.IsAssetsPresent(root));

            // The archive is cleaned up after a successful install.
            Assert.False(File.Exists(downloader.ArchivePath));

            // Progress was reported for both phases.
            Assert.Contains(reported, p => p.Phase == AssetInstallPhase.Downloading);
            Assert.Contains(reported, p => p.Phase == AssetInstallPhase.Extracting);
            Assert.Contains(reported, p => p.Phase == AssetInstallPhase.Done);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReportsAlreadyPresentWhenAssetsExist()
    {
        string root = CreateTempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "assets", "data", "songs", "bopeebo"));
            var downloader = new AssetDownloader(AssetSource.Default, root);
            var result = await downloader.InstallAsync();

            Assert.Equal(AssetInstallStatus.AlreadyPresent, result.Status);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task CancellingKeepsThePartialDownloadForResume()
    {
        // Large enough that the socket buffers cannot swallow it, so cancellation lands mid-stream.
        byte[] zip = BuildArchive(Enumerable.Range(0, 32).ToDictionary(
            i => $"preload/data/file{i}.bin",
            _ => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48 * 1024))));

        using var server = new ZipServer(zip, delayPerChunkMs: 4, chunkSize: 4096);
        string root = CreateTempRoot();

        try
        {
            var downloader = new AssetDownloader(new AssetSource("test", "assets", "main", server.Url), root);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

            var result = await downloader.InstallAsync(null, cts.Token);

            Assert.Equal(AssetInstallStatus.Cancelled, result.Status);
            Assert.True(File.Exists(downloader.ArchivePath), "the partial archive should be kept for resuming");

            // A second attempt without cancellation finishes the job (the server honours ranges).
            var retry = await downloader.InstallAsync();
            Assert.True(retry.Status == AssetInstallStatus.Success, $"retry status={retry.Status} message={retry.Message}");
            Assert.True(File.Exists(Path.Combine(root, "assets", "data", "file31.bin")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task IgnoresEntriesWhichEscapeTheAssetsFolder()
    {
        byte[] zip = BuildArchive(new Dictionary<string, string>
        {
            ["preload/data/ok.json"] = "{}",
            ["preload/../../evil.txt"] = "nope",
        });

        using var server = new ZipServer(zip);
        string root = CreateTempRoot();

        try
        {
            var downloader = new AssetDownloader(new AssetSource("test", "assets", "main", server.Url), root);
            var result = await downloader.InstallAsync();

            Assert.Equal(AssetInstallStatus.Success, result.Status);
            Assert.True(File.Exists(Path.Combine(root, "assets", "data", "ok.json")));
            Assert.False(File.Exists(Path.Combine(root, "evil.txt")));
            Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "evil.txt")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReportsNetworkErrors()
    {
        string root = CreateTempRoot();
        try
        {
            using var server = new ZipServer(Array.Empty<byte>(), statusCode: 404);
            var downloader = new AssetDownloader(new AssetSource("test", "assets", "main", server.Url), root);

            var result = await downloader.InstallAsync();

            Assert.Equal(AssetInstallStatus.NetworkError, result.Status);
            Assert.Contains("404", result.Message);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "funkin-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>Builds a zipball-shaped archive: one top level folder plus the given entries.</summary>
    private static byte[] BuildArchive(Dictionary<string, string> entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, contents) in entries)
            {
                var entry = archive.CreateEntry($"Funkin.assets-main/{path}");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(contents);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Minimal HTTP server used to exercise the downloader: serves a zip, optionally throttled,
    /// and supports Range requests so resume behaviour can be tested.
    /// </summary>
    private sealed class ZipServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly byte[] _payload;
        private readonly int _delayPerChunkMs;
        private readonly int _chunkSize;
        private readonly int _statusCode;
        private readonly Task _loop;

        public string Url { get; }

        public ZipServer(byte[] payload, int delayPerChunkMs = 0, int statusCode = 200, int chunkSize = 8192)
        {
            _payload = payload;
            _delayPerChunkMs = delayPerChunkMs;
            _statusCode = statusCode;
            _chunkSize = chunkSize;

            int port = GetFreePort();
            Url = $"http://127.0.0.1:{port}/assets.zip";
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _loop = Task.Run(ServeAsync);
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch
                {
                    return;
                }

                _ = Task.Run(() => Respond(context));
            }
        }

        private async Task Respond(HttpListenerContext context)
        {
            try
            {
                if (_statusCode != 200)
                {
                    context.Response.StatusCode = _statusCode;
                    context.Response.Close();
                    return;
                }

                long start = 0;
                string? range = context.Request.Headers["Range"];
                if (range is not null && range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
                {
                    string spec = range[6..];
                    int dash = spec.IndexOf('-');
                    if (dash > 0 && long.TryParse(spec[..dash], out long parsed)) start = parsed;
                }

                if (start >= _payload.Length) start = 0;

                context.Response.StatusCode = start > 0 ? 206 : 200;
                context.Response.ContentType = "application/zip";
                context.Response.ContentLength64 = _payload.Length - start;

                if (start > 0)
                {
                    context.Response.Headers.Add("Content-Range", $"bytes {start}-{_payload.Length - 1}/{_payload.Length}");
                }

                for (long offset = start; offset < _payload.Length; offset += _chunkSize)
                {
                    int count = (int)Math.Min(_chunkSize, _payload.Length - offset);
                    await context.Response.OutputStream.WriteAsync(_payload.AsMemory((int)offset, count));
                    await context.Response.OutputStream.FlushAsync();
                    if (_delayPerChunkMs > 0) await Task.Delay(_delayPerChunkMs);
                }

                context.Response.Close();
            }
            catch
            {
                try { context.Response.Abort(); } catch { /* client went away */ }
            }
        }

        public void Dispose()
        {
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
                // ignore
            }
        }
    }
}
