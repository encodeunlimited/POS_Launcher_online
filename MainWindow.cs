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
    private readonly ServerManager? _serverManager;
    private readonly string _startUrl;
    private readonly FlowLayoutPanel _overlayPanel;

    public MainWindow(LauncherConfig config, ServerManager? serverManager, string startUrl)
    {
        _config = config;
        _serverManager = serverManager;
        _startUrl = startUrl;

        // ── Window properties ──────────────────────────────────────────────
        Text = config.AppTitle;
        Width = config.WindowWidth;
        Height = config.WindowHeight;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 18);   // Dark bg while loading

        // ── Kiosk Mode properties ──────────────────────────────────────────
        void ApplyKioskMode(bool kiosk)
        {
            if (kiosk)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = true;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _config.StartMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
                TopMost = false;
            }
        }

        ApplyKioskMode(_config.EnableKioskMode);

        // ── Application Icon ──────────────────────────────────────────────
        ApplyApplicationIcon();

        // ── WebView2 control ───────────────────────────────────────────────
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false   // Hidden until page loads (avoids white flash)
        };
        Controls.Add(_webView);

        // ── Sliding Toolbar Panel ──────────────────────────────────────────
        _overlayPanel = new FlowLayoutPanel
        {
            BackColor = Color.FromArgb(40, 40, 40),
            WrapContents = false
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

        bool isKioskActive = _config.EnableKioskMode;

        _overlayPanel.Controls.Add(CreateToolBtn("✕", Color.FromArgb(200, 50, 50), (s, e) => Close()));
        _overlayPanel.Controls.Add(CreateToolBtn("_", Color.FromArgb(80, 80, 80), (s, e) => WindowState = FormWindowState.Minimized));
        
        var kioskBtn = CreateToolBtn("⛶", Color.FromArgb(80, 80, 80), (s, e) => 
        {
            isKioskActive = !isKioskActive;
            ApplyKioskMode(isKioskActive);
        });
        _overlayPanel.Controls.Add(kioskBtn);

        _overlayPanel.Controls.Add(CreateToolBtn("↻", Color.FromArgb(80, 80, 80), (s, e) => _webView.Reload()));
        _overlayPanel.Controls.Add(CreateToolBtn("+", Color.FromArgb(80, 80, 80), (s, e) => { if (_webView.ZoomFactor < 3.0) _webView.ZoomFactor += 0.1; }));
        _overlayPanel.Controls.Add(CreateToolBtn("-", Color.FromArgb(80, 80, 80), (s, e) => { if (_webView.ZoomFactor > 0.3) _webView.ZoomFactor -= 0.1; }));
        _overlayPanel.Controls.Add(CreateToolBtn("⚙", Color.FromArgb(80, 80, 80), (s, e) => 
        {
            using var settingsForm = new SettingsForm(_config, () => {
                _webView.ZoomFactor = _config.DefaultZoomLevel;
                Text = _config.AppTitle;
                UpdateControlPanelLayout();
                ApplyApplicationIcon();
            }, _serverManager);
            if (Icon != null)
                settingsForm.Icon = Icon;
            settingsForm.ShowDialog(this);
        }));

        Controls.Add(_overlayPanel);
        UpdateControlPanelLayout();

        var slideTimer = new System.Windows.Forms.Timer { Interval = 20 };
        slideTimer.Tick += (s, e) =>
        {
            if (WindowState == FormWindowState.Minimized || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            var cursorPos = PointToClient(Cursor.Position);
            bool isHovering = _overlayPanel.Bounds.Contains(cursorPos);
            string pos = (_config.ControlPanelPosition ?? "Right").Trim().ToLowerInvariant();
            const int step = 20;

            if (pos == "left")
            {
                _overlayPanel.Top = (ClientSize.Height - _overlayPanel.Height) / 2;
                int targetX = isHovering ? 0 : (10 - _overlayPanel.Width);
                if (_overlayPanel.Left < targetX)
                    _overlayPanel.Left = Math.Min(_overlayPanel.Left + step, targetX);
                else if (_overlayPanel.Left > targetX)
                    _overlayPanel.Left = Math.Max(_overlayPanel.Left - step, targetX);
            }
            else if (pos == "top")
            {
                _overlayPanel.Left = (ClientSize.Width - _overlayPanel.Width) / 2;
                int targetY = isHovering ? 0 : (10 - _overlayPanel.Height);
                if (_overlayPanel.Top < targetY)
                    _overlayPanel.Top = Math.Min(_overlayPanel.Top + step, targetY);
                else if (_overlayPanel.Top > targetY)
                    _overlayPanel.Top = Math.Max(_overlayPanel.Top - step, targetY);
            }
            else if (pos == "bottom")
            {
                _overlayPanel.Left = (ClientSize.Width - _overlayPanel.Width) / 2;
                int targetY = isHovering ? (ClientSize.Height - _overlayPanel.Height) : (ClientSize.Height - 10);
                if (_overlayPanel.Top < targetY)
                    _overlayPanel.Top = Math.Min(_overlayPanel.Top + step, targetY);
                else if (_overlayPanel.Top > targetY)
                    _overlayPanel.Top = Math.Max(_overlayPanel.Top - step, targetY);
            }
            else // "right"
            {
                _overlayPanel.Top = (ClientSize.Height - _overlayPanel.Height) / 2;
                int targetX = isHovering ? (ClientSize.Width - _overlayPanel.Width) : (ClientSize.Width - 10);
                if (_overlayPanel.Left < targetX)
                    _overlayPanel.Left = Math.Min(_overlayPanel.Left + step, targetX);
                else if (_overlayPanel.Left > targetX)
                    _overlayPanel.Left = Math.Max(_overlayPanel.Left - step, targetX);
            }
        };
        slideTimer.Start();

        // ── Loading overlay (spinner while WebView2 initialises) ───────────
        var splash = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 18)
        };
        string modeText = _config.IsOffline ? "Offline Mode" : "Online Mode";
        var loadLabel = new Label
        {
            Text = $"Starting {_config.AppTitle} ({modeText})...",
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
        FormClosed += (s, e) => 
        {
            try { _serverManager?.Dispose(); } catch { }
            try { _webView.Dispose(); } catch { }
            Environment.Exit(0);
        };
        Load += async (s, e) =>
        {
            await InitWebViewAsync();
            splash.Visible = false;
            _webView.Visible = true;
        };

        Activated += (s, e) =>
        {
            if (_webView.Visible && _webView.CoreWebView2 != null)
            {
                BeginInvoke(new Action(() => {
                    _webView.Focus();
                    _webView.CoreWebView2.ExecuteScriptAsync("window.focus(); if (document.activeElement) document.activeElement.focus();");
                }));
            }
        };

        Resize += (s, e) =>
        {
            if (WindowState != FormWindowState.Minimized && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                var cursorPos = PointToClient(Cursor.Position);
                bool isHovering = _overlayPanel.Bounds.Contains(cursorPos);
                if (!isHovering)
                {
                    string pos = (_config.ControlPanelPosition ?? "Right").Trim().ToLowerInvariant();
                    if (pos == "right")
                    {
                        _overlayPanel.Left = ClientSize.Width - 10;
                        _overlayPanel.Top = (ClientSize.Height - _overlayPanel.Height) / 2;
                    }
                    else if (pos == "left")
                    {
                        _overlayPanel.Left = 10 - _overlayPanel.Width;
                        _overlayPanel.Top = (ClientSize.Height - _overlayPanel.Height) / 2;
                    }
                    else if (pos == "top")
                    {
                        _overlayPanel.Left = (ClientSize.Width - _overlayPanel.Width) / 2;
                        _overlayPanel.Top = 10 - _overlayPanel.Height;
                    }
                    else if (pos == "bottom")
                    {
                        _overlayPanel.Left = (ClientSize.Width - _overlayPanel.Width) / 2;
                        _overlayPanel.Top = ClientSize.Height - 10;
                    }
                }

                if (_webView.Visible && _webView.CoreWebView2 != null)
                {
                    BeginInvoke(new Action(() => {
                        _webView.Focus();
                        _webView.CoreWebView2.ExecuteScriptAsync("window.focus(); if (document.activeElement) document.activeElement.focus();");
                    }));
                }
            }
        };
    }

    private void UpdateControlPanelLayout()
    {
        string pos = (_config.ControlPanelPosition ?? "Right").Trim().ToLowerInvariant();
        bool isVertical = pos == "left" || pos == "right";

        if (isVertical)
        {
            _overlayPanel.FlowDirection = FlowDirection.TopDown;
            _overlayPanel.Size = new Size(60, 360);
            _overlayPanel.Padding = new Padding(10, 10, 10, 0);
            foreach (Control ctrl in _overlayPanel.Controls)
            {
                ctrl.Margin = new Padding(0, 0, 0, 10);
            }
            _overlayPanel.Top = (ClientSize.Height - _overlayPanel.Height) / 2;
            _overlayPanel.Left = pos == "left" ? (10 - _overlayPanel.Width) : (ClientSize.Width - 10);
        }
        else
        {
            _overlayPanel.FlowDirection = FlowDirection.LeftToRight;
            _overlayPanel.Size = new Size(360, 60);
            _overlayPanel.Padding = new Padding(10, 10, 0, 10);
            foreach (Control ctrl in _overlayPanel.Controls)
            {
                ctrl.Margin = new Padding(0, 0, 10, 0);
            }
            _overlayPanel.Left = (ClientSize.Width - _overlayPanel.Width) / 2;
            _overlayPanel.Top = pos == "top" ? (10 - _overlayPanel.Height) : (ClientSize.Height - 10);
        }

        _overlayPanel.BringToFront();
    }

    private void ApplyApplicationIcon()
    {
        try
        {
            string? iconPath = _config.IconPath;
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                string fullPath = Path.IsPathRooted(iconPath)
                    ? iconPath
                    : Path.Combine(AppContext.BaseDirectory, iconPath);

                if (!File.Exists(fullPath) && File.Exists(Path.Combine(Environment.CurrentDirectory, iconPath)))
                {
                    fullPath = Path.Combine(Environment.CurrentDirectory, iconPath);
                }

                if (File.Exists(fullPath))
                {
                    Icon = new Icon(fullPath);
                    return;
                }
            }

            // Fallback to embedded icon in exe
            if (Icon.ExtractAssociatedIcon(Application.ExecutablePath) is Icon appIcon)
            {
                Icon = appIcon;
            }
        }
        catch { /* Fallback to default WinForms icon if extraction fails */ }
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
        
        _webView.ZoomFactor = _config.DefaultZoomLevel;

        // ── Harden the WebView2 settings ──────────────────────────────────
        var settings = _webView.CoreWebView2.Settings;
        settings.IsStatusBarEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;  // no right-click menu
        settings.IsZoomControlEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false; // disable F12, Ctrl+U etc.
        settings.IsSwipeNavigationEnabled = false;

        // Allow dev tools with Ctrl+Shift+I if needed for debugging
        // settings.AreDevToolsEnabled = true;

        // ── Local Printer Extraction & Injection ───────────────────────────
        var localPrinters = GetInstalledPrinters();
        string printersJson = System.Text.Json.JsonSerializer.Serialize(localPrinters);

        // Inject window.localPcPrinters and dispatch event before any page script executes
        string injectScript = $@"
(function() {{
    window.posLauncher = {{
        isLauncher: true,
        version: '1.0.0',
        printers: {printersJson}
    }};
    window.localPcPrinters = {printersJson};
    try {{
        window.dispatchEvent(new CustomEvent('pos-launcher-ready', {{
            detail: {{ printers: {printersJson} }}
        }}));
    }} catch(e) {{}}
}})();";

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(injectScript);

        // WebMessage handler for on-demand requests from the web app
        _webView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            try
            {
                var msg = e.TryGetWebMessageAsString();
                if (msg == "get-printers" || msg == "refresh-printers")
                {
                    var updated = GetInstalledPrinters();
                    var json = System.Text.Json.JsonSerializer.Serialize(new { type = "printers-list", printers = updated });
                    _webView.CoreWebView2.PostWebMessageAsJson(json);
                }
            }
            catch { }
        };

        // Auto-sync printers to the web app backend whenever navigation completes
        _webView.CoreWebView2.NavigationCompleted += async (s, e) =>
        {
            if (e.IsSuccess && localPrinters.Count > 0)
            {
                string syncScript = $@"
(function() {{
    try {{
        fetch('/api/printers/sync', {{
            method: 'POST',
            headers: {{ 'Content-Type': 'application/json' }},
            body: JSON.stringify({{ printers: {printersJson} }})
        }}).catch(function(err) {{ }});
    }} catch(e) {{}}
}})();";
                try
                {
                    await _webView.CoreWebView2.ExecuteScriptAsync(syncScript);
                }
                catch { }
            }
        };

        // ── Navigate to the app ────────────────────────────────────────────
        _webView.CoreWebView2.Navigate(_startUrl);
    }

    private static List<string> GetInstalledPrinters()
    {
        var list = new List<string>();
        try
        {
            foreach (string p in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    list.Add(p.Trim());
                }
            }
        }
        catch { }
        return list;
    }
}

