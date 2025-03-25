using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.IO;
using System.Text.Json;

namespace CheaterV1
{
    public partial class Form1 : BaseProtectedForm
    {
        private IKeyboardMouseEvents hook;
        private Rectangle rect;
        private bool isRectVisible = false;
        private bool isDragging = false;
        private Point mouseOffset;
        private AppSettings settings;
        private HoTro hoTro;

        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        public Form1()
        {
            InitializeComponent();
            settings = AppSettings.Load();
            ApplySettings();

            richTextBox1.ScrollBars = RichTextBoxScrollBars.None;
            richTextBox1.MouseWheel += RichTextBox1_MouseWheel;
            richTextBox1.KeyDown += RichTextBox1_KeyDown;
            richTextBox1.TextChanged += RichTextBox1_TextChanged;

            hook = Hook.GlobalEvents();
            hook.KeyDown += Hook_KeyDown;
            this.FormBorderStyle = FormBorderStyle.None;
            Load += Form1_Load;

            hoTro = new HoTro(); // Khởi tạo HoTro để chạy toàn cục
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            const uint WDA_NONE = 0;
            const uint WDA_MONITOR = 1;
            SetWindowDisplayAffinity(this.Handle, WDA_MONITOR);
        }

        private async void RunTask()
        {
            await Task.Run(() => { });
        }

        private void RichTextBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                richTextBox1.SelectionStart = Math.Max(0, richTextBox1.SelectionStart - 20);
            }
            else
            {
                richTextBox1.SelectionStart = Math.Min(richTextBox1.Text.Length, richTextBox1.SelectionStart + 20);
            }
        }

        private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.SuppressKeyPress = true;
                if (Clipboard.ContainsText())
                {
                    string plainText = Clipboard.GetText();
                    int start = richTextBox1.SelectionStart;
                    richTextBox1.Text = richTextBox1.Text.Insert(start, plainText);
                    richTextBox1.SelectionStart = start + plainText.Length;
                    ApplyTextColor();
                }
            }
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            ApplyTextColor();
        }

        private void ApplyTextColor()
        {
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = settings.GetTextColor();
            richTextBox1.DeselectAll();
        }

        private void Hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                ToggleVisibility();
            }
            if (e.KeyCode == Keys.Down)
            {
                this.FormBorderStyle = (this.FormBorderStyle == FormBorderStyle.SizableToolWindow) ?
                    FormBorderStyle.None : FormBorderStyle.SizableToolWindow;
            }
            if (e.KeyCode == Keys.Insert)
            {
                new setting().Show();
            }
            if (e.KeyCode == Keys.RControlKey)
            {
                Environment.Exit(0);
            }
            // Xóa xử lý F1, F2, F3 ở đây vì HoTro sẽ xử lý toàn cục
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isRectVisible)
            {
                using (Pen pen = new Pen(Color.Blue, 3))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (isRectVisible && rect.Contains(e.Location))
            {
                isDragging = true;
                mouseOffset = new Point(e.X - rect.X, e.Y - rect.Y);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                rect.Location = new Point(e.X - mouseOffset.X, e.Y - mouseOffset.Y);
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            hook.Dispose();
            hoTro.Dispose();
            base.OnFormClosing(e);
        }

        public void ApplySettings()
        {
            settings = AppSettings.Load();
            this.BackColor = settings.GetBackgroundColor();
            this.ForeColor = settings.GetTextColor();
            this.Opacity = settings.GetOpacity();
            this.TransparencyKey = settings.GetIsTransparent() ? Color.Magenta : Color.Empty;
            ApplySettingsToControls(this);
            ApplyTextColor();
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
    }
}