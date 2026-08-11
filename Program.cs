using System.Text.Json;
using System.IO;

namespace POS_launcher;

/// <summary>
/// Application entry point.
/// IMPORTANT: Must be a synchronous [STAThread] method — async Task Main() breaks
/// the COM STA apartment required by WebView2 (RPC_E_CHANGED_MODE).
/// Async server startup is offloaded to Task.Run() to avoid STA deadlock.
/// </summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ── Load config ────────────────────────────────────────────────────
        string exeDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(exeDir, "launcher.json");
        LauncherConfig config;

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
        }
        else
        {
            config = new LauncherConfig();
        }

        // ── Single-instance guard ──────────────────────────────────────────
        using var mutex = new System.Threading.Mutex(true, $"{config.AppTitle}_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                $"\"{config.AppTitle}\" is already running.",
                config.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // ── Start the PHP Desktop server ───────────────────────────────────
        // Run on a background thread via Task.Run so we don't deadlock the
        // STA main thread with async continuations returning to the wrong context.
        var server = new ServerManager(exeDir, config);
        string serverUrl;

        try
        {
            if (!string.IsNullOrWhiteSpace(config.StartUrl))
            {
                // Fixed-port mode: wait until port is ready, then use known URL
                Task.Run(async () =>
                    await server.StartWithFixedUrlAsync(config.StartUrl!))
                    .GetAwaiter().GetResult();
                serverUrl = config.StartUrl!;
            }
            else
            {
                // Auto-detect mode: read port from debug.log
                serverUrl = Task.Run(async () =>
                    await server.StartAsync())
                    .GetAwaiter().GetResult();
            }
        }
        catch (FileNotFoundException ex)
        {
            server.Dispose();
            MessageBox.Show(
                ex.Message + $"\n\nMake sure {config.ServerExe} is in the same folder as the launcher.",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        catch (TimeoutException ex)
        {
            server.Dispose();
            MessageBox.Show(
                ex.Message,
                "Startup Timeout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        catch (Exception ex)
        {
            server.Dispose();
            MessageBox.Show(
                $"Failed to start server:\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // ── Launch the desktop window (blocks until window is closed) ──────
        Application.Run(new MainWindow(config, server, serverUrl));
    }
}
