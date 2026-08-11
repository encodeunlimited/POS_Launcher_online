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

        // ── Sliding Toolbar Panel ──────────────────────────────────────────
        var overlayPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            BackColor = Color.FromArgb(40, 40, 40),
            Width = 60,
            Height = 260,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(ClientSize.Width - 10, (ClientSize.Height - 260) / 2),
            Padding = new Padding(10, 10, 10, 0)
        };

        Button CreateToolBtn(string text, Color backColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 40,
                Height = 40,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        overlayPanel.Controls.Add(CreateToolBtn("✕", Color.FromArgb(200, 50, 50), (s, e) => Close()));
        overlayPanel.Controls.Add(CreateToolBtn("_", Color.FromArgb(80, 80, 80), (s, e) => WindowState = FormWindowState.Minimized));
        overlayPanel.Controls.Add(CreateToolBtn("↻", Color.FromArgb(80, 80, 80), (s, e) => _webView.Reload()));
        overlayPanel.Controls.Add(CreateToolBtn("+", Color.FromArgb(80, 80, 80), (s, e) => { if (_webView.ZoomFactor < 3.0) _webView.ZoomFactor += 0.1; }));
        overlayPanel.Controls.Add(CreateToolBtn("-", Color.FromArgb(80, 80, 80), (s, e) => { if (_webView.ZoomFactor > 0.3) _webView.ZoomFactor -= 0.1; }));

        Controls.Add(overlayPanel);
        overlayPanel.BringToFront();

        var slideTimer = new System.Windows.Forms.Timer { Interval = 20 };
        slideTimer.Tick += (s, e) =>
        {
            var cursorPos = PointToClient(Cursor.Position);
            bool isHovering = overlayPanel.Bounds.Contains(cursorPos);
            int targetX = isHovering ? ClientSize.Width - overlayPanel.Width : ClientSize.Width - 10;
            
            if (overlayPanel.Left != targetX)
            {
                int step = isHovering ? -20 : 20;
                int newX = overlayPanel.Left + step;
                if ((step < 0 && newX < targetX) || (step > 0 && newX > targetX))
                    newX = targetX;
                overlayPanel.Left = newX;
            }
        };
        slideTimer.Start();

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
