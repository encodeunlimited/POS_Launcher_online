using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace POS_launcher;

/// <summary>
/// Manages the PHP built-in web server process lifecycle for Offline mode.
/// Starts "php.exe -S 127.0.0.1:PORT -t www www/index.php" in background,
/// polls TCP until the port is ready, and terminates the process tree on close.
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

    /// <summary>
    /// Starts the server exe and polls until the fixed port is listening.
    /// </summary>
    public async Task StartWithFixedUrlAsync(string startUrl, CancellationToken ct = default)
    {
        string serverExe = _config.ServerExe ?? "php/php.exe";
        string serverPath = Path.IsPathRooted(serverExe)
            ? serverExe
            : Path.Combine(_exeDir, serverExe);

        if (!File.Exists(serverPath))
        {
            string altPath = Path.Combine(Environment.CurrentDirectory, serverExe);
            if (File.Exists(altPath))
            {
                serverPath = altPath;
            }
            else
            {
                throw new FileNotFoundException($"Server executable not found at:\n{serverPath}");
            }
        }

        // Parse host and port from URL (e.g. "http://127.0.0.1:8080/")
        string host = "127.0.0.1";
        int port = 8080;
        try
        {
            if (Uri.TryCreate(startUrl, UriKind.Absolute, out var uri))
            {
                host = string.IsNullOrWhiteSpace(uri.Host) ? "127.0.0.1" : uri.Host;
                port = uri.Port > 0 ? uri.Port : 80;
            }
        }
        catch { }

        // If port is already open and accepting connections, reuse existing server instance
        if (IsPortOpen(host, port))
        {
            return;
        }

        // Build arguments: expand {port} token if present
        string args = _config.ServerArgs ?? $"-S {host}:{port} -t www www/index.php";
        args = args.Replace("{port}", port.ToString());

        StartProcess(serverPath, args);

        // Poll until TCP port is ready
        int timeoutSeconds = _config.WaitTimeoutSeconds > 0 ? _config.WaitTimeoutSeconds : 30;
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (IsPortOpen(host, port))
                return;

            if (_serverProcess != null && _serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Server process terminated unexpectedly with exit code {_serverProcess.ExitCode}.\n" +
                    $"Command: {serverPath} {args}");
            }

            await Task.Delay(250, ct);
        }

        Kill();
        throw new TimeoutException(
            $"Server did not respond on {host}:{port} within {timeoutSeconds} seconds.\n" +
            $"Check that {serverPath} exists and runs properly.");
    }

    private void StartProcess(string exePath, string arguments)
    {
        string workingDir = _exeDir;
        if (!Directory.Exists(workingDir))
        {
            workingDir = AppContext.BaseDirectory;
        }

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            CreateNoWindow = !_config.ShowConsole,
            UseShellExecute = false,
            WindowStyle = _config.ShowConsole
                ? ProcessWindowStyle.Normal
                : ProcessWindowStyle.Hidden,
        };

        _serverProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start server process.");
    }

    public static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(300));
            if (success)
            {
                client.EndConnect(result);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Kill()
    {
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(2000);
            }
        }
        catch { }

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
                catch { }
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
