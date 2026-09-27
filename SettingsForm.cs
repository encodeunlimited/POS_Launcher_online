using System;
using System.Drawing;
using System.Windows.Forms;

namespace POS_launcher;

public class SettingsForm : Form
{
    private LauncherConfig _config;
    private TextBox _txtUrl;
    private NumericUpDown _numZoom;
    private CheckBox _chkKiosk;
    private Action _onSettingsSaved;

    public SettingsForm(LauncherConfig config, Action onSettingsSaved)
    {
        _config = config;
        _onSettingsSaved = onSettingsSaved;

        Text = "Launcher Settings";
        Width = 400;
        Height = 250;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        TopMost = true;

        var lblUrl = new Label { Text = "Start URL:", Left = 20, Top = 20, Width = 100 };
        _txtUrl = new TextBox { Left = 120, Top = 20, Width = 240, Text = _config.StartUrl };

        var lblZoom = new Label { Text = "Default Zoom:", Left = 20, Top = 60, Width = 100 };
        _numZoom = new NumericUpDown
        {
            Left = 120,
            Top = 60,
            Width = 100,
            Minimum = 0.3m,
            Maximum = 3.0m,
            DecimalPlaces = 1,
            Increment = 0.1m,
            Value = (decimal)_config.DefaultZoomLevel
        };

        _chkKiosk = new CheckBox
        {
            Text = "Enable Kiosk Mode on Startup",
            Left = 20,
            Top = 100,
            Width = 300,
            Checked = _config.EnableKioskMode
        };

        var btnSave = new Button { Text = "Save", Left = 120, Top = 150, Width = 100 };
        btnSave.Click += (s, e) => SaveSettings();

        var btnCancel = new Button { Text = "Cancel", Left = 230, Top = 150, Width = 100 };
        btnCancel.Click += (s, e) => Close();

        Controls.Add(lblUrl);
        Controls.Add(_txtUrl);
        Controls.Add(lblZoom);
        Controls.Add(_numZoom);
        Controls.Add(_chkKiosk);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void SaveSettings()
    {
        _config.StartUrl = _txtUrl.Text;
        _config.DefaultZoomLevel = (double)_numZoom.Value;
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
