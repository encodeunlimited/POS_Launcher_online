using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POS_launcher;

/// <summary>
/// Launcher configuration loaded from launcher.json next to the exe.
/// Supports both Offline (local PHP server) and Online (remote web URL) modes.
/// </summary>
public class LauncherConfig
{
    [JsonPropertyName("AppTitle")]
    public string AppTitle { get; set; } = "POS TB OR";

    /// <summary>
    /// Operating mode: "Offline" or "Online".
    /// If null or empty, automatically inferred:
    /// if ServerExe is defined -> "Offline", otherwise "Online".
    /// </summary>
    [JsonPropertyName("Mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// The active navigation URL.
    /// </summary>
    [JsonPropertyName("StartUrl")]
    public string? StartUrl { get; set; }

    /// <summary>
    /// Target URL when running in Online mode.
    /// </summary>
    [JsonPropertyName("OnlineUrl")]
    public string? OnlineUrl { get; set; } = "https://bonds.encodeunlimited.com/";

    /// <summary>
    /// Target URL when running in Offline mode.
    /// </summary>
    [JsonPropertyName("OfflineUrl")]
    public string? OfflineUrl { get; set; } = "http://127.0.0.1:8080/";

    /// <summary>
    /// Path to the local PHP or web server executable.
    /// Example: "php/php.exe"
    /// </summary>
    [JsonPropertyName("ServerExe")]
    public string? ServerExe { get; set; } = "php/php.exe";

    /// <summary>
    /// Command-line arguments passed to the server executable.
    /// Example: "-S 127.0.0.1:8080 -t www www/index.php"
    /// </summary>
    [JsonPropertyName("ServerArgs")]
    public string? ServerArgs { get; set; } = "-S 127.0.0.1:8080 -t www www/index.php";

    [JsonPropertyName("WaitTimeoutSeconds")]
    public int WaitTimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("ShowConsole")]
    public bool ShowConsole { get; set; } = false;

    [JsonPropertyName("WindowWidth")]
    public int WindowWidth { get; set; } = 1366;

    [JsonPropertyName("WindowHeight")]
    public int WindowHeight { get; set; } = 768;

    [JsonPropertyName("StartMaximized")]
    public bool StartMaximized { get; set; } = true;

    [JsonPropertyName("EnableKioskMode")]
    public bool EnableKioskMode { get; set; } = true;

    [JsonPropertyName("DefaultZoomLevel")]
    public double DefaultZoomLevel { get; set; } = 1.0;

    [JsonPropertyName("ControlPanelPosition")]
    public string ControlPanelPosition { get; set; } = "Right";

    [JsonPropertyName("IconPath")]
    public string? IconPath { get; set; } = "cafe_icon.ico";

    /// <summary>
    /// Returns true if configured or inferred to be running in Offline mode.
    /// </summary>
    [JsonIgnore]
    public bool IsOffline
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Mode))
            {
                return Mode.Trim().Equals("Offline", StringComparison.OrdinalIgnoreCase);
            }
            // Inferred mode: if ServerExe is specified, default to Offline
            return !string.IsNullOrWhiteSpace(ServerExe);
        }
    }

    /// <summary>
    /// Returns the resolved URL to open on launch based on the mode.
    /// </summary>
    public string GetEffectiveStartUrl()
    {
        if (IsOffline)
        {
            if (!string.IsNullOrWhiteSpace(OfflineUrl))
                return OfflineUrl;
            if (!string.IsNullOrWhiteSpace(StartUrl) && StartUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                return StartUrl;
            return "http://127.0.0.1:8080/";
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(OnlineUrl))
                return OnlineUrl;
            if (!string.IsNullOrWhiteSpace(StartUrl))
                return StartUrl;
            return "https://www.example.com/";
        }
    }

    public void Save()
    {
        string exeDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(exeDir, "launcher.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(configPath, json);
    }
}
