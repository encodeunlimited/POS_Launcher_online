using System.Text.Json.Serialization;

namespace POS_launcher;

/// <summary>
/// Launcher configuration loaded from launcher.json next to the exe.
/// </summary>
public class LauncherConfig
{
    [JsonPropertyName("AppTitle")]
    public string AppTitle { get; set; } = "System Launcher";

    /// <summary>
    /// The URL the launcher navigates directly to.
    /// </summary>
    [JsonPropertyName("StartUrl")]
    public string? StartUrl { get; set; } = "https://www.example.com/";

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

    public void Save()
    {
        string exeDir = System.AppContext.BaseDirectory;
        string configPath = System.IO.Path.Combine(exeDir, "launcher.json");
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        string json = System.Text.Json.JsonSerializer.Serialize(this, options);
        System.IO.File.WriteAllText(configPath, json);
    }
}
