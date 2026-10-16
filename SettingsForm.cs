using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace POS_launcher;

public class SettingsForm : Form
{
    private LauncherConfig _config;
    private TextBox _txtUrl;
    private NumericUpDown _numZoom;
    private ComboBox _cmbPosition;
    private ComboBox _cmbIcon;
    private Button _btnBrowseIcon;
    private PictureBox _picIconPreview;
    private CheckBox _chkKiosk;
    private Action _onSettingsSaved;

    public SettingsForm(LauncherConfig config, Action onSettingsSaved)
    {
        _config = config;
        _onSettingsSaved = onSettingsSaved;

        Text = "Launcher Settings";
        Width = 460;
        Height = 330;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        TopMost = true;

        var lblUrl = new Label { Text = "Start URL:", Left = 20, Top = 20, Width = 110 };
        _txtUrl = new TextBox { Left = 140, Top = 20, Width = 280, Text = _config.StartUrl };

        var lblZoom = new Label { Text = "Default Zoom:", Left = 20, Top = 60, Width = 110 };
        _numZoom = new NumericUpDown
        {
            Left = 140,
            Top = 60,
            Width = 100,
            Minimum = 0.3m,
            Maximum = 3.0m,
            DecimalPlaces = 1,
            Increment = 0.1m,
            Value = (decimal)_config.DefaultZoomLevel
        };

        var lblPosition = new Label { Text = "Panel Position:", Left = 20, Top = 100, Width = 110 };
        _cmbPosition = new ComboBox
        {
            Left = 140,
            Top = 98,
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPosition.Items.AddRange(new object[] { "Top", "Bottom", "Left", "Right" });
        int selPosIdx = _cmbPosition.FindStringExact(_config.ControlPanelPosition);
        _cmbPosition.SelectedIndex = selPosIdx >= 0 ? selPosIdx : _cmbPosition.FindStringExact("Right");
        if (_cmbPosition.SelectedIndex < 0 && _cmbPosition.Items.Count > 0)
        {
            _cmbPosition.SelectedIndex = 3;
        }

        var lblIcon = new Label { Text = "Launcher Icon:", Left = 20, Top = 140, Width = 110 };
        _cmbIcon = new ComboBox
        {
            Left = 140,
            Top = 138,
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _btnBrowseIcon = new Button
        {
            Text = "Browse...",
            Left = 308,
            Top = 137,
            Width = 75,
            Height = 25
        };

        _picIconPreview = new PictureBox
        {
            Left = 392,
            Top = 136,
            Width = 28,
            Height = 28,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        // Populate available icons
        var iconFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string exeDir = AppContext.BaseDirectory;
        if (Directory.Exists(exeDir))
        {
            foreach (var f in Directory.GetFiles(exeDir, "*.ico"))
            {
                iconFiles.Add(Path.GetFileName(f));
            }
        }

        string currentDir = Environment.CurrentDirectory;
        if (Directory.Exists(currentDir) && !currentDir.Equals(exeDir, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var f in Directory.GetFiles(currentDir, "*.ico"))
            {
                iconFiles.Add(Path.GetFileName(f));
            }
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

        _chkKiosk = new CheckBox
        {
            Text = "Enable Kiosk Mode on Startup",
            Left = 20,
            Top = 180,
            Width = 300,
            Checked = _config.EnableKioskMode
        };

        var btnSave = new Button { Text = "Save", Left = 170, Top = 230, Width = 100, Height = 32 };
        btnSave.Click += (s, e) => SaveSettings();

        var btnCancel = new Button { Text = "Cancel", Left = 285, Top = 230, Width = 100, Height = 32 };
        btnCancel.Click += (s, e) => Close();

        FormClosed += (s, e) => _picIconPreview.Image?.Dispose();

        Controls.Add(lblUrl);
        Controls.Add(_txtUrl);
        Controls.Add(lblZoom);
        Controls.Add(_numZoom);
        Controls.Add(lblPosition);
        Controls.Add(_cmbPosition);
        Controls.Add(lblIcon);
        Controls.Add(_cmbIcon);
        Controls.Add(_btnBrowseIcon);
        Controls.Add(_picIconPreview);
        Controls.Add(_chkKiosk);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void SaveSettings()
    {
        _config.StartUrl = _txtUrl.Text;
        _config.DefaultZoomLevel = (double)_numZoom.Value;
        _config.ControlPanelPosition = _cmbPosition.SelectedItem?.ToString() ?? "Right";
        _config.IconPath = _cmbIcon.SelectedItem?.ToString();
        _config.EnableKioskMode = _chkKiosk.Checked;
        
        try
        {
            _config.Save();
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _onSettingsSaved?.Invoke();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
