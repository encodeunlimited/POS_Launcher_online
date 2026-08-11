using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.IO;

namespace POS_launcher;

/// <summary>
/// The main application window — a frameless, maximizable Windows Form
/// hosting a WebView2 control that shows the web app.
/// </summary>
public class MainWindow : Form
{
    private readonly WebView2 _webView;
    private readonly LauncherConfig _config;
    private readonly ServerManager _server;
    private readonly string _startUrl;

    public MainWindow(LauncherConfig config, ServerManager server, string startUrl)
    {
        _config = config;
        _server = server;
        _startUrl = startUrl;

        // ── Window properties ──────────────────────────────────────────────
        Text = config.AppTitle;
        Width = config.WindowWidth;
        Height = config.WindowHeight;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 18);   // Dark bg while loading

        FormBorderStyle = FormBorderStyle.None; // True Kiosk Mode
        WindowState = FormWindowState.Maximized;
        TopMost = true; // Ensure it covers the taskbar and stays on top

        // Automatically use the icon embedded in the .exe for the taskbar
        try
        {
            if (Icon.ExtractAssociatedIcon(Application.ExecutablePath) is Icon appIcon)
            {
                Icon = appIcon;
            }
        }
        catch { /* Fallback to default WinForms icon if extraction fails */ }

        // ── WebView2 control ───────────────────────────────────────────────
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false   // Hidden until page loads (avoids white flash)
        };
        Controls.Add(_webView);

        // ── Close Button ───────────────────────────────────────────────────
        var closeBtn = new Button
        {
            Text = "✕",
            BackColor = Color.FromArgb(200, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.Location = new Point(ClientSize.Width - closeBtn.Width - 10, 10);
        closeBtn.Click += (s, e) => Close();
        Controls.Add(closeBtn);
        closeBtn.BringToFront();

        // ── Zoom In Button ─────────────────────────────────────────────────
        var zoomInBtn = new Button
        {
            Text = "+",
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        zoomInBtn.FlatAppearance.BorderSize = 0;
        zoomInBtn.Location = new Point(ClientSize.Width - 230, ClientSize.Height - zoomInBtn.Height - 30);
        zoomInBtn.Click += (s, e) => { if (_webView.ZoomFactor < 3.0) _webView.ZoomFactor += 0.1; };
        Controls.Add(zoomInBtn);
        zoomInBtn.BringToFront();

        // ── Zoom Out Button ────────────────────────────────────────────────
        var zoomOutBtn = new Button
        {
            Text = "-",
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(40, 40),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        zoomOutBtn.FlatAppearance.BorderSize = 0;
        zoomOutBtn.Location = new Point(ClientSize.Width - 180, ClientSize.Height - zoomOutBtn.Height - 30);
        zoomOutBtn.Click += (s, e) => { if (_webView.ZoomFactor > 0.3) _webView.ZoomFactor -= 0.1; };
        Controls.Add(zoomOutBtn);
        zoomOutBtn.BringToFront();

        // ── Refresh Button ─────────────────────────────────────────────────
        var refreshBtn = new Button
        {
            Text = "↻ Refresh",
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 40),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        refreshBtn.FlatAppearance.BorderSize = 0;
        refreshBtn.Location = new Point(ClientSize.Width - 130, ClientSize.Height - refreshBtn.Height - 30);
        refreshBtn.Click += (s, e) => _webView.Reload();
        Controls.Add(refreshBtn);
        refreshBtn.BringToFront();

        // ── Loading overlay (spinner while WebView2 initialises) ───────────
        var splash = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 18)
        };
        var loadLabel = new Label
        {
            Text = $"Starting {_config.AppTitle}...",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            AutoSize = true
        };
        splash.Controls.Add(loadLabel);
        splash.Paint += (s, e) =>
        {
            loadLabel.Left = (splash.Width - loadLabel.Width) / 2;
            loadLabel.Top = (splash.Height - loadLabel.Height) / 2;
        };
        Controls.Add(splash);
        splash.BringToFront();

        // ── Wire up events ─────────────────────────────────────────────────
        FormClosing += (s, e) => _server.Kill();
        Load += async (s, e) =>
        {
            await InitWebViewAsync();
            splash.Visible = false;
            _webView.Visible = true;
        };
    }

    private async Task InitWebViewAsync()
    {
        // Store WebView2 user data next to the exe
        string userDataFolder = Path.Combine(AppContext.BaseDirectory, "wv2data");
        var options = new CoreWebView2EnvironmentOptions();
        options.AdditionalBrowserArguments = "--kiosk-printing";

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder,
            options: options);

        await _webView.EnsureCoreWebView2Async(env);

        // ── Harden the WebView2 settings ──────────────────────────────────
        var settings = _webView.CoreWebView2.Settings;
        settings.IsStatusBarEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;  // no right-click menu
        settings.IsZoomControlEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false; // disable F12, Ctrl+U etc.
        settings.IsSwipeNavigationEnabled = false;

        // Allow dev tools with Ctrl+Shift+I if needed for debugging
        // settings.AreDevToolsEnabled = true;

        // ── Navigate to the app ────────────────────────────────────────────
        _webView.CoreWebView2.Navigate(_startUrl);
    }
}
