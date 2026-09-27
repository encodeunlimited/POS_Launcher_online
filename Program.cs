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

        // ── Get the Start URL ──────────────────────────────────────────────
        string serverUrl = string.IsNullOrWhiteSpace(config.StartUrl)
            ? "https://www.example.com/"
            : config.StartUrl;

        // ── Launch the desktop window (blocks until window is closed) ──────
        Application.Run(new MainWindow(config, serverUrl));
    }
}
