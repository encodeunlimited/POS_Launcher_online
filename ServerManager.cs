using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace POS_launcher;

/// <summary>
/// Manages the PHP built-in web server process lifecycle.
/// Starts "php.exe -S 127.0.0.1:PORT -t www" hidden in background,
/// polls TCP until the port is ready, and kills the process on close.
/// </summary>
public class ServerManager : IDisposable
{
    private Process? _serverProcess;
    private readonly string _exeDir;
    private readonly LauncherConfig _config;
    private bool _disposed;

    public ServerManager(string exeDir, LauncherConfig config)
    {
        _exeDir = exeDir;
        _config = config;
    }

    // ── Fixed-port mode ───────────────────────────────────────────────────

    /// <summary>
    /// Starts the server exe (hidden) and polls until the fixed port is listening.
    /// </summary>
    public async Task StartWithFixedUrlAsync(string startUrl, CancellationToken ct = default)
    {
        string serverPath = Path.Combine(_exeDir, _config.ServerExe);
        if (!File.Exists(serverPath))
            throw new FileNotFoundException($"Server exe not found: {serverPath}");

        // Parse port from URL, e.g. "http://127.0.0.1:8080/"
        var uri = new Uri(startUrl);
        int port = uri.Port;

        // Build arguments: if ServerArgs is defined use it, else build default
        string args = _config.ServerArgs ?? $"-S 127.0.0.1:{port} -t www";
        // Expand {port} token
        args = args.Replace("{port}", port.ToString());

        StartProcess(serverPath, args);

        // Poll until the TCP port accepts connections
        var deadline = DateTime.UtcNow.AddSeconds(_config.WaitTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (IsPortOpen("127.0.0.1", port))
                return;
            await Task.Delay(300, ct);
        }

        Kill();
        throw new TimeoutException(
            $"Server did not start within {_config.WaitTimeoutSeconds} seconds. " +
            $"Check that {_config.ServerExe} exists and is correct.");
    }

    // ── Auto-detect mode (debug.log parsing) ─────────────────────────────

    /// <summary>
    /// Starts the server exe (hidden) and returns the detected URL by reading debug.log.
    /// Primarily for phpdesktop-chrome.exe.
    /// </summary>
    public async Task<string> StartAsync(CancellationToken ct = default)
    {
        string serverPath = Path.Combine(_exeDir, _config.ServerExe);
        if (!File.Exists(serverPath))
            throw new FileNotFoundException($"Server exe not found: {serverPath}");

        string debugLogPath = Path.Combine(_exeDir, "debug.log");
        if (File.Exists(debugLogPath))
            File.Delete(debugLogPath);

        StartProcess(serverPath, _config.ServerArgs);

        // Regex to match: "Web server url: http://127.0.0.1:PORT/"
        var portRegex = new Regex(@"Web server url: http://127\.0\.0\.1:(\d+)/");

        var deadline = DateTime.UtcNow.AddSeconds(_config.WaitTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (File.Exists(debugLogPath))
            {
                string log = SafeReadLog(debugLogPath);
                var m = portRegex.Match(log);
                if (m.Success)
                    return $"http://127.0.0.1:{m.Groups[1].Value}/";
            }

            await Task.Delay(300, ct);
        }

        Kill();
        throw new TimeoutException(
            $"Server did not start within {_config.WaitTimeoutSeconds} seconds. " +
            $"Check that {_config.ServerExe} exists and settings.json is correct.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void StartProcess(string exePath, string? arguments = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = _exeDir,
            CreateNoWindow = !_config.ShowConsole,
            UseShellExecute = false,
            WindowStyle = _config.ShowConsole
                ? ProcessWindowStyle.Normal
                : ProcessWindowStyle.Hidden,
        };

        _serverProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start server process.");
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(300));
            if (success) client.EndConnect(result);
            return success;
        }
        catch { return false; }
    }

    private static string SafeReadLog(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            return reader.ReadToEnd();
        }
        catch { return string.Empty; }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Kill()
    {
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(3000);
            }
        }
        catch { /* best effort */ }

        // Fallback: cleanup any orphaned background processes running from this directory
        CleanupOrphanedProcesses("php-cgi");
        CleanupOrphanedProcesses("php");
        CleanupOrphanedProcesses("mysqld");
        CleanupOrphanedProcesses("mariadbd");
        CleanupOrphanedProcesses("nginx");
        CleanupOrphanedProcesses("httpd");
    }

    private void CleanupOrphanedProcesses(string processName)
    {
        try
        {
            foreach (var p in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (p.MainModule != null && 
                        p.MainModule.FileName.StartsWith(_exeDir, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kill();
                    }
                }
                catch { /* Ignore access denied on processes we don't own */ }
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Kill();
        _serverProcess?.Dispose();
    }
}
