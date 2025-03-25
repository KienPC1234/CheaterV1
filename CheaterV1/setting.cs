using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;

namespace CheaterV1
{
    public class SerializableColor
    {
        public int A { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public SerializableColor() { }

        public SerializableColor(Color color)
        {
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }
    }

    public class AppSettings
    {
        public SerializableColor BackgroundColor { get; set; } = new SerializableColor(Color.White);
        public SerializableColor TextColor { get; set; } = new SerializableColor(Color.Black);
        public double Opacity { get; set; } = 1.0;
        public bool IsTransparent { get; set; } = false;
        private static readonly string SettingsFile = "settings.json";

        public static AppSettings Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                    return loadedSettings ?? new AppSettings();
                }
                catch { }
            }
            return new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
            MessageBox.Show("Saved settings: " + json);
        }

        public Color GetBackgroundColor() => BackgroundColor.ToColor();
        public Color GetTextColor() => TextColor.ToColor();
        public double GetOpacity() => Opacity;
        public bool GetIsTransparent() => IsTransparent;
    }

    public partial class setting : Form
    {
        private AppSettings settings;
        private Button btnBackgroundColor;
        private Button btnTextColor;
        private Button btnSave;
        private TrackBar trackBarOpacity;
        private CheckBox chkTransparent;

        public setting()
        {
            InitializeComponent();
            settings = AppSettings.Load();
            InitializeComponents();
            ApplySettings();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(600, 400);

            btnBackgroundColor = new Button { Text = "Chọn màu nền", Dock = DockStyle.Top, Height = 32 };
            btnTextColor = new Button { Text = "Chọn màu chữ", Dock = DockStyle.Top, Height = 32 };
            btnSave = new Button { Text = "Lưu cài đặt", Dock = DockStyle.Top, Height = 32 };
            trackBarOpacity = new TrackBar
            {
                Dock = DockStyle.Top,
                Minimum = 10,
                Maximum = 100,
                Value = (int)(settings.Opacity * 100),
                Height = 40
            };
            chkTransparent = new CheckBox
            {
                Text = "Trong suốt",
                Dock = DockStyle.Top,
                Height = 32,
                Checked = settings.IsTransparent
            };

            btnBackgroundColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        settings.BackgroundColor = new SerializableColor(cd.Color);
                        ApplySettings();
                    }
                }
            };

            btnTextColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        settings.TextColor = new SerializableColor(cd.Color);
                        ApplySettings();
                    }
                }
            };

            trackBarOpacity.Scroll += (s, e) =>
            {
                settings.Opacity = trackBarOpacity.Value / 100.0;
                ApplySettings();
            };

            chkTransparent.CheckedChanged += (s, e) =>
            {
                settings.IsTransparent = chkTransparent.Checked;
                if (settings.IsTransparent)
                {
                    settings.BackgroundColor = new SerializableColor(Color.Magenta);
                }
                ApplySettings();
            };

            btnSave.Click += (s, e) =>
            {
                settings.Save();
                ApplySettingsToMainForm();
                this.Close();
            };

            Controls.Add(chkTransparent);
            Controls.Add(trackBarOpacity);
            Controls.Add(btnSave);
            Controls.Add(btnTextColor);
            Controls.Add(btnBackgroundColor);
        }

        public void ApplySettings()
        {
            this.BackColor = settings.GetBackgroundColor();
            this.ForeColor = settings.GetTextColor();
            this.Opacity = settings.GetOpacity();
            this.TransparencyKey = settings.IsTransparent ? Color.Magenta : Color.Empty;
            ApplySettingsToControls(this);
        }

        private void ApplySettingsToControls(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                ctrl.BackColor = settings.GetBackgroundColor();
                ctrl.ForeColor = settings.GetTextColor();
                if (ctrl.HasChildren)
                {
                    ApplySettingsToControls(ctrl);
                }
            }
        }

        private void ApplySettingsToMainForm()
        {
            if (Application.OpenForms["Form1"] is Form1 mainForm)
            {
                mainForm.ApplySettings();
            }
        }

        private void setting_Load(object sender, EventArgs e)
        {
            ApplySettings();
        }
    }
}