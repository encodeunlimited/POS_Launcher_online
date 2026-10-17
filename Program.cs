using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POS_launcher;

/// <summary>
/// Application entry point.
/// IMPORTANT: Must be a synchronous [STAThread] method — async Task Main() breaks
/// the COM STA apartment required by WebView2 (RPC_E_CHANGED_MODE).
/// Server startup is offloaded to Task.Run() to avoid STA deadlock.
/// </summary>
internal static class Program
{
    public static Mutex? SingleInstanceMutex;

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
            try
            {
                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
            }
            catch
            {
                config = new LauncherConfig();
            }
        }
        else
        {
            config = new LauncherConfig();
            try { config.Save(); } catch { }
        }

        // ── Single-instance guard ──────────────────────────────────────────
        SingleInstanceMutex = new Mutex(true, $"{config.AppTitle}_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                $"\"{config.AppTitle}\" is already running.",
                config.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        string targetUrl = config.GetEffectiveStartUrl();
        ServerManager? server = null;

        // ── Offline PHP Server Startup ─────────────────────────────────────
        if (config.IsOffline)
        {
            server = new ServerManager(exeDir, config);
            try
            {
                Task.Run(async () => await server.StartWithFixedUrlAsync(targetUrl))
                    .GetAwaiter().GetResult();
            }
            catch (FileNotFoundException ex)
            {
                server.Dispose();
                var result = MessageBox.Show(
                    $"{ex.Message}\n\nWould you like to open Settings to configure the server path or switch to Online mode?",
                    "Offline Server Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    Application.Run(new SettingsForm(config, null, null));
                }
                return;
            }
            catch (Exception ex)
            {
                server.Dispose();
                var result = MessageBox.Show(
                    $"Failed to start offline server:\n{ex.Message}\n\nWould you like to open Settings to check your configuration?",
                    "Offline Server Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    Application.Run(new SettingsForm(config, null, null));
                }
                return;
            }
        }

        // ── Launch the desktop window ──────────────────────────────────────
        Application.Run(new MainWindow(config, server, targetUrl));
    }

    /// <summary>
    /// Gracefully releases the single instance mutex and restarts the application.
    /// </summary>
    public static void RestartApplication()
    {
        try
        {
            SingleInstanceMutex?.ReleaseMutex();
            SingleInstanceMutex?.Dispose();
            SingleInstanceMutex = null;
        }
        catch { }

        Application.Restart();
        Environment.Exit(0);
    }
}
