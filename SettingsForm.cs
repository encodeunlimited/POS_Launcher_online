using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace POS_launcher;

public class SettingsForm : Form
{
    private readonly LauncherConfig _config;
    private readonly Action? _onSettingsSaved;
    private readonly ServerManager? _serverManager;

    // Mode controls
    private RadioButton _rbOffline;
    private RadioButton _rbOnline;
    private Label _lblModeHint;

    // URL controls
    private TextBox _txtOfflineUrl;
    private TextBox _txtOnlineUrl;

    // Server controls
    private GroupBox _grpServer;
    private TextBox _txtServerExe;
    private Button _btnBrowseExe;
    private TextBox _txtServerArgs;
    private NumericUpDown _numTimeout;
    private CheckBox _chkShowConsole;

    // Appearance controls
    private TextBox _txtAppTitle;
    private NumericUpDown _numZoom;
    private ComboBox _cmbPosition;
    private ComboBox _cmbIcon;
    private Button _btnBrowseIcon;
    private PictureBox _picIconPreview;
    private CheckBox _chkKiosk;
    private CheckBox _chkMaximized;

    public SettingsForm(LauncherConfig config, Action? onSettingsSaved, ServerManager? serverManager = null)
    {
        _config = config;
        _onSettingsSaved = onSettingsSaved;
        _serverManager = serverManager;

        Text = "Launcher Settings (Offline & Online)";
        Width = 560;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        TopMost = true;

        int currentY = 15;
        const int leftLabel = 15;
        const int leftInput = 145;
        const int inputWidth = 360;

        // ════════════════════════════════════════════════════════════════════
        // 1. LAUNCHER MODE
        // ════════════════════════════════════════════════════════════════════
        var grpMode = new GroupBox
        {
            Text = "Launcher Mode",
            Left = 15,
            Top = currentY,
            Width = 515,
            Height = 85,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold)
        };

        bool isCurrentlyOffline = _config.IsOffline;

        _rbOffline = new RadioButton
        {
            Text = "Offline Mode (Local PHP Desktop Server)",
            Left = 20,
            Top = 24,
            Width = 270,
            Checked = isCurrentlyOffline,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };

        _rbOnline = new RadioButton
        {
            Text = "Online Mode (Remote Web URL)",
            Left = 300,
            Top = 24,
            Width = 200,
            Checked = !isCurrentlyOffline,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };

        _lblModeHint = new Label
        {
            Left = 20,
            Top = 54,
            Width = 480,
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };

        grpMode.Controls.Add(_rbOffline);
        grpMode.Controls.Add(_rbOnline);
        grpMode.Controls.Add(_lblModeHint);
        Controls.Add(grpMode);

        currentY += grpMode.Height + 10;

        // ════════════════════════════════════════════════════════════════════
        // 2. TARGET URLS
        // ════════════════════════════════════════════════════════════════════
        var grpUrls = new GroupBox
        {
            Text = "Target URLs",
            Left = 15,
            Top = currentY,
            Width = 515,
            Height = 95,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold)
        };

        var lblOfflineUrl = new Label
        {
            Text = "Offline URL:",
            Left = leftLabel,
            Top = 25,
            Width = 120,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _txtOfflineUrl = new TextBox
        {
            Left = leftInput,
            Top = 22,
            Width = inputWidth,
            Text = !string.IsNullOrWhiteSpace(_config.OfflineUrl)
                ? _config.OfflineUrl
                : (!string.IsNullOrWhiteSpace(_config.StartUrl) && _config.StartUrl.Contains("127.0.0.1") ? _config.StartUrl : "http://127.0.0.1:8080/"),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        var lblOnlineUrl = new Label
        {
            Text = "Online URL:",
            Left = leftLabel,
            Top = 58,
            Width = 120,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _txtOnlineUrl = new TextBox
        {
            Left = leftInput,
            Top = 55,
            Width = inputWidth,
            Text = !string.IsNullOrWhiteSpace(_config.OnlineUrl)
                ? _config.OnlineUrl
                : (!string.IsNullOrWhiteSpace(_config.StartUrl) && !_config.StartUrl.Contains("127.0.0.1") ? _config.StartUrl : "https://bonds.encodeunlimited.com/"),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        grpUrls.Controls.Add(lblOfflineUrl);
        grpUrls.Controls.Add(_txtOfflineUrl);
        grpUrls.Controls.Add(lblOnlineUrl);
        grpUrls.Controls.Add(_txtOnlineUrl);
        Controls.Add(grpUrls);

        currentY += grpUrls.Height + 10;

        // ════════════════════════════════════════════════════════════════════
        // 3. OFFLINE PHP SERVER SETTINGS
        // ════════════════════════════════════════════════════════════════════
        _grpServer = new GroupBox
        {
            Text = "Offline Server Configuration (PHP Desktop)",
            Left = 15,
            Top = currentY,
            Width = 515,
            Height = 135,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold)
        };

        var lblServerExe = new Label
        {
            Text = "Server Executable:",
            Left = leftLabel,
            Top = 26,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _txtServerExe = new TextBox
        {
            Left = leftInput,
            Top = 23,
            Width = inputWidth - 85,
            Text = _config.ServerExe ?? "php/php.exe",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _btnBrowseExe = new Button
        {
            Text = "Browse...",
            Left = leftInput + inputWidth - 80,
            Top = 22,
            Width = 80,
            Height = 26,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };

        var lblServerArgs = new Label
        {
            Text = "Server Arguments:",
            Left = leftLabel,
            Top = 60,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _txtServerArgs = new TextBox
        {
            Left = leftInput,
            Top = 57,
            Width = inputWidth,
            Text = _config.ServerArgs ?? "-S 127.0.0.1:8080 -t www www/index.php",
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        var lblTimeout = new Label
        {
            Text = "Timeout (seconds):",
            Left = leftLabel,
            Top = 95,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _numTimeout = new NumericUpDown
        {
            Left = leftInput,
            Top = 93,
            Width = 80,
            Minimum = 5,
            Maximum = 120,
            Value = Math.Clamp(_config.WaitTimeoutSeconds, 5, 120),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        _chkShowConsole = new CheckBox
        {
            Text = "Show server console window (Debug)",
            Left = leftInput + 95,
            Top = 93,
            Width = 260,
            Checked = _config.ShowConsole,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        _btnBrowseExe.Click += (s, e) =>
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Server Executable (e.g. php.exe)",
                Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
                InitialDirectory = AppContext.BaseDirectory
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string rel = Path.GetRelativePath(AppContext.BaseDirectory, ofd.FileName);
                _txtServerExe.Text = rel.StartsWith("..") ? ofd.FileName : rel.Replace('\\', '/');
            }
        };

        _grpServer.Controls.Add(lblServerExe);
        _grpServer.Controls.Add(_txtServerExe);
        _grpServer.Controls.Add(_btnBrowseExe);
        _grpServer.Controls.Add(lblServerArgs);
        _grpServer.Controls.Add(_txtServerArgs);
        _grpServer.Controls.Add(lblTimeout);
        _grpServer.Controls.Add(_numTimeout);
        _grpServer.Controls.Add(_chkShowConsole);
        Controls.Add(_grpServer);

        currentY += _grpServer.Height + 10;

        // ════════════════════════════════════════════════════════════════════
        // 4. APPEARANCE & DISPLAY SETTINGS
        // ════════════════════════════════════════════════════════════════════
        var grpApp = new GroupBox
        {
            Text = "Window & Appearance",
            Left = 15,
            Top = currentY,
            Width = 515,
            Height = 160,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Bold)
        };

        var lblAppTitle = new Label
        {
            Text = "Application Title:",
            Left = leftLabel,
            Top = 26,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _txtAppTitle = new TextBox
        {
            Left = leftInput,
            Top = 23,
            Width = inputWidth,
            Text = _config.AppTitle,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        var lblPosition = new Label
        {
            Text = "Panel Position:",
            Left = leftLabel,
            Top = 58,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _cmbPosition = new ComboBox
        {
            Left = leftInput,
            Top = 55,
            Width = 110,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _cmbPosition.Items.AddRange(new object[] { "Top", "Bottom", "Left", "Right" });
        int selPosIdx = _cmbPosition.FindStringExact(_config.ControlPanelPosition);
        _cmbPosition.SelectedIndex = selPosIdx >= 0 ? selPosIdx : 3;

        var lblZoom = new Label
        {
            Text = "Default Zoom:",
            Left = 280,
            Top = 58,
            Width = 95,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _numZoom = new NumericUpDown
        {
            Left = 380,
            Top = 55,
            Width = 70,
            Minimum = 0.3m,
            Maximum = 3.0m,
            DecimalPlaces = 1,
            Increment = 0.1m,
            Value = Math.Clamp((decimal)_config.DefaultZoomLevel, 0.3m, 3.0m),
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        var lblIcon = new Label
        {
            Text = "Launcher Icon:",
            Left = leftLabel,
            Top = 92,
            Width = 125,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };
        _cmbIcon = new ComboBox
        {
            Left = leftInput,
            Top = 89,
            Width = 190,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular)
        };

        _btnBrowseIcon = new Button
        {
            Text = "Browse...",
            Left = leftInput + 200,
            Top = 88,
            Width = 75,
            Height = 26,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };

        _picIconPreview = new PictureBox
        {
            Left = leftInput + 285,
            Top = 87,
            Width = 28,
            Height = 28,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _chkKiosk = new CheckBox
        {
            Text = "Enable Kiosk Mode on Startup",
            Left = leftInput,
            Top = 124,
            Width = 200,
            Checked = _config.EnableKioskMode,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        _chkMaximized = new CheckBox
        {
            Text = "Start Maximized",
            Left = leftInput + 210,
            Top = 124,
            Width = 150,
            Checked = _config.StartMaximized,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        grpApp.Controls.Add(lblAppTitle);
        grpApp.Controls.Add(_txtAppTitle);
        grpApp.Controls.Add(lblPosition);
        grpApp.Controls.Add(_cmbPosition);
        grpApp.Controls.Add(lblZoom);
        grpApp.Controls.Add(_numZoom);
        grpApp.Controls.Add(lblIcon);
        grpApp.Controls.Add(_cmbIcon);
        grpApp.Controls.Add(_btnBrowseIcon);
        grpApp.Controls.Add(_picIconPreview);
        grpApp.Controls.Add(_chkKiosk);
        grpApp.Controls.Add(_chkMaximized);
        Controls.Add(grpApp);

        currentY += grpApp.Height + 15;

        // ════════════════════════════════════════════════════════════════════
        // 5. ACTION BUTTONS
        // ════════════════════════════════════════════════════════════════════
        var btnSave = new Button
        {
            Text = "Save Settings",
            Left = 280,
            Top = currentY,
            Width = 120,
            Height = 34,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (s, e) => SaveSettings();

        var btnCancel = new Button
        {
            Text = "Cancel",
            Left = 410,
            Top = currentY,
            Width = 120,
            Height = 34,
            BackColor = Color.FromArgb(220, 220, 220),
            ForeColor = Color.FromArgb(30, 30, 30),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => Close();

        Controls.Add(btnSave);
        Controls.Add(btnCancel);

        // Populate available icons
        PopulateIcons();

        // Mode switch event handler
        void UpdateModeUI()
        {
            if (_rbOffline.Checked)
            {
                _lblModeHint.Text = "Offline Mode: Starts the local PHP server and loads the local offline URL.";
                _grpServer.Enabled = true;
                _txtOfflineUrl.BackColor = Color.White;
                _txtOnlineUrl.BackColor = Color.FromArgb(245, 245, 245);
            }
            else
            {
                _lblModeHint.Text = "Online Mode: Navigates directly to the online web URL without starting local PHP.";
                _grpServer.Enabled = false;
                _txtOnlineUrl.BackColor = Color.White;
                _txtOfflineUrl.BackColor = Color.FromArgb(245, 245, 245);
            }
        }

        _rbOffline.CheckedChanged += (s, e) => UpdateModeUI();
        _rbOnline.CheckedChanged += (s, e) => UpdateModeUI();
        UpdateModeUI();

        FormClosed += (s, e) => _picIconPreview.Image?.Dispose();
    }

    private void PopulateIcons()
    {
        var iconFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string exeDir = AppContext.BaseDirectory;
        if (Directory.Exists(exeDir))
        {
            foreach (var f in Directory.GetFiles(exeDir, "*.ico"))
                iconFiles.Add(Path.GetFileName(f));
        }

        string currentDir = Environment.CurrentDirectory;
        if (Directory.Exists(currentDir) && !currentDir.Equals(exeDir, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var f in Directory.GetFiles(currentDir, "*.ico"))
                iconFiles.Add(Path.GetFileName(f));
        }

        if (!string.IsNullOrWhiteSpace(_config.IconPath))
        {
            iconFiles.Add(_config.IconPath);
        }

        foreach (var ico in iconFiles)
        {
            _cmbIcon.Items.Add(ico);
        }

        if (!string.IsNullOrWhiteSpace(_config.IconPath))
        {
            int idx = _cmbIcon.FindStringExact(_config.IconPath);
            if (idx >= 0)
                _cmbIcon.SelectedIndex = idx;
            else if (_cmbIcon.Items.Count > 0)
                _cmbIcon.SelectedIndex = 0;
        }
        else if (_cmbIcon.Items.Count > 0)
        {
            _cmbIcon.SelectedIndex = 0;
        }

        void UpdateIconPreview()
        {
            try
            {
                string? selected = _cmbIcon.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    string fullPath = Path.IsPathRooted(selected)
                        ? selected
                        : Path.Combine(AppContext.BaseDirectory, selected);

                    if (!File.Exists(fullPath) && File.Exists(Path.Combine(Environment.CurrentDirectory, selected)))
                    {
                        fullPath = Path.Combine(Environment.CurrentDirectory, selected);
                    }

                    if (File.Exists(fullPath))
                    {
                        using var ico = new Icon(fullPath, 32, 32);
                        _picIconPreview.Image?.Dispose();
                        _picIconPreview.Image = ico.ToBitmap();
                        return;
                    }
                }

                if (Icon.ExtractAssociatedIcon(Application.ExecutablePath) is Icon appIcon)
                {
                    _picIconPreview.Image?.Dispose();
                    _picIconPreview.Image = appIcon.ToBitmap();
                }
            }
            catch
            {
                _picIconPreview.Image?.Dispose();
                _picIconPreview.Image = null;
            }
        }

        _cmbIcon.SelectedIndexChanged += (s, e) => UpdateIconPreview();
        UpdateIconPreview();

        _btnBrowseIcon.Click += (s, e) =>
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Application Icon",
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*",
                InitialDirectory = AppContext.BaseDirectory
            };

            if (ofd.ShowDialog(this) == DialogResult.OK && File.Exists(ofd.FileName))
            {
                string selectedFile = ofd.FileName;
                string fileName = Path.GetFileName(selectedFile);
                string targetPath = Path.Combine(AppContext.BaseDirectory, fileName);

                try
                {
                    if (!selectedFile.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(selectedFile, targetPath, overwrite: true);
                    }
                    if (!_cmbIcon.Items.Contains(fileName))
                    {
                        _cmbIcon.Items.Add(fileName);
                    }
                    _cmbIcon.SelectedItem = fileName;
                }
                catch
                {
                    if (!_cmbIcon.Items.Contains(selectedFile))
                    {
                        _cmbIcon.Items.Add(selectedFile);
                    }
                    _cmbIcon.SelectedItem = selectedFile;
                }
            }
        };
    }

    private void SaveSettings()
    {
        string oldMode = _config.IsOffline ? "Offline" : "Online";
        string oldServerExe = _config.ServerExe ?? "";
        string oldServerArgs = _config.ServerArgs ?? "";
        string oldUrl = _config.GetEffectiveStartUrl();
        bool oldShowConsole = _config.ShowConsole;

        bool newIsOffline = _rbOffline.Checked;
        string newMode = newIsOffline ? "Offline" : "Online";
        string newOfflineUrl = _txtOfflineUrl.Text.Trim();
        string newOnlineUrl = _txtOnlineUrl.Text.Trim();
        string newServerExe = _txtServerExe.Text.Trim();
        string newServerArgs = _txtServerArgs.Text.Trim();
        bool newShowConsole = _chkShowConsole.Checked;

        _config.Mode = newMode;
        _config.OfflineUrl = newOfflineUrl;
        _config.OnlineUrl = newOnlineUrl;
        _config.StartUrl = newIsOffline ? newOfflineUrl : newOnlineUrl;
        _config.ServerExe = newServerExe;
        _config.ServerArgs = newServerArgs;
        _config.WaitTimeoutSeconds = (int)_numTimeout.Value;
        _config.ShowConsole = newShowConsole;

        _config.AppTitle = _txtAppTitle.Text.Trim();
        _config.DefaultZoomLevel = (double)_numZoom.Value;
        _config.ControlPanelPosition = _cmbPosition.SelectedItem?.ToString() ?? "Right";
        _config.IconPath = _cmbIcon.SelectedItem?.ToString();
        _config.EnableKioskMode = _chkKiosk.Checked;
        _config.StartMaximized = _chkMaximized.Checked;

        try
        {
            _config.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string newUrl = _config.GetEffectiveStartUrl();
        bool modeChanged = !string.Equals(oldMode, newMode, StringComparison.OrdinalIgnoreCase);
        bool serverChanged = !string.Equals(oldServerExe, newServerExe, StringComparison.OrdinalIgnoreCase) ||
                             !string.Equals(oldServerArgs, newServerArgs, StringComparison.OrdinalIgnoreCase) ||
                             oldShowConsole != newShowConsole;
        bool urlChanged = !string.Equals(oldUrl, newUrl, StringComparison.OrdinalIgnoreCase);

        if (modeChanged || serverChanged || urlChanged)
        {
            var res = MessageBox.Show(
                "Settings saved successfully!\n\nMode, Server, or URL settings have changed. Would you like to restart the launcher now to apply these changes?",
                "Restart Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                try { _serverManager?.Kill(); } catch { }
                Program.RestartApplication();
                return;
            }
        }
        else
        {
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _onSettingsSaved?.Invoke();
        Close();
    }
}
