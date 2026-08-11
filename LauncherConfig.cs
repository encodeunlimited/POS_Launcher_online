using System.Text.Json.Serialization;

namespace POS_launcher;

/// <summary>
/// Launcher configuration loaded from launcher.json next to the exe.
/// </summary>
public class LauncherConfig
{
    [JsonPropertyName("AppTitle")]
    public string AppTitle { get; set; } = "System Launcher";

    [JsonPropertyName("ServerExe")]
    public string ServerExe { get; set; } = "server.exe";

    /// <summary>
    /// If set, the launcher navigates directly to this URL instead of
    /// auto-detecting the port from debug.log.
    /// </summary>
    [JsonPropertyName("StartUrl")]
    public string? StartUrl { get; set; } = null;

    /// <summary>
    /// Optional command-line arguments passed to the server exe.
    /// Use {port} as a token for the port number.
    /// Example: "-S 127.0.0.1:{port} -t www"
    /// </summary>
    [JsonPropertyName("ServerArgs")]
    public string? ServerArgs { get; set; } = null;

    [JsonPropertyName("WindowWidth")]
    public int WindowWidth { get; set; } = 1366;

    [JsonPropertyName("WindowHeight")]
    public int WindowHeight { get; set; } = 768;

    [JsonPropertyName("StartMaximized")]
    public bool StartMaximized { get; set; } = true;

    [JsonPropertyName("WaitTimeoutSeconds")]
    public int WaitTimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("ShowConsole")]
    public bool ShowConsole { get; set; } = false;
}
