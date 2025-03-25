using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace CheaterV1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }

    public class BaseProtectedForm : Form
    {
        private Thread protectionThread;
        private Thread topmostThread;
        private volatile bool isRunning = true;
        private IntPtr hookId = IntPtr.Zero;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        private const int WH_CBT = 5;
        private const int HCBT_ACTIVATE = 5;
        private const int HCBT_SETFOCUS = 7;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int WM_DESTROY = 0x0002;
        private const int WM_QUIT = 0x0012;
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int WM_SIZE = 0x0005;
        private const int WM_MOVE = 0x0003;
        private const int WM_SETTEXT = 0x000C;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;

        public BaseProtectedForm()
        {
            this.TopMost = true;

            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST | WS_EX_TOOLWINDOW);
            SetWindowPos(this.Handle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowDisplayAffinity(this.Handle, 1);

            MARGINS margins = new MARGINS { leftWidth = -1, rightWidth = -1, topHeight = -1, bottomHeight = -1 };
            DwmExtendFrameIntoClientArea(this.Handle, ref margins);

            hookId = SetHook(HookCallback);

            topmostThread = new Thread(KeepTopmost) { Priority = ThreadPriority.Highest, IsBackground = true };
            topmostThread.Start();

            protectionThread = new Thread(ProtectProcess) { Priority = ThreadPriority.Highest };
            protectionThread.Start();
        }

        private IntPtr SetHook(HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_CBT, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (nCode == HCBT_ACTIVATE || nCode == HCBT_SETFOCUS))
            {
                IntPtr activatedWindow = wParam;
                if (activatedWindow != this.Handle && this.Visible)
                {
                    BringWindowToTop(this.Handle);
                    SetForegroundWindow(this.Handle);
                    SetWindowPos(this.Handle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private void KeepTopmost()
        {
            while (isRunning)
            {
                if (this.Visible)
                {
                    if (!this.TopMost || GetForegroundWindow() != this.Handle)
                    {
                        this.TopMost = true;
                        BringWindowToTop(this.Handle);
                        SetForegroundWindow(this.Handle);
                        SetWindowPos(this.Handle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
                    }
                }
                Thread.Sleep(1); 
            }
        }

        private void ProtectProcess()
        {
            string appPath = Application.ExecutablePath;
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, currentProcess.Id);
            while (isRunning)
            {
                try
                {
                    Process.GetProcessById(currentProcess.Id);
                }
                catch
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = appPath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo);
                    break;
                }
                Thread.Sleep(500);
            }
            CloseHandle(processHandle);
        }

        public void ToggleVisibility()
        {
            if (this.Visible)
            {
                ShowWindow(this.Handle, 0);
                this.Visible = false;
            }
            else
            {
                ShowWindow(this.Handle, 5);
                this.Visible = true;
                SetWindowPos(this.Handle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                BringWindowToTop(this.Handle);
                SetForegroundWindow(this.Handle);
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {

            if (m.Msg == WM_DESTROY || m.Msg == WM_QUIT || m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MINIMIZE ||
                m.Msg == WM_SIZE || m.Msg == WM_MOVE || m.Msg == WM_SETTEXT)
            {
                return;
            }
            if (m.Msg == WM_WINDOWPOSCHANGING && this.Visible)
            {
                WINDOWPOS wp = (WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));
                if (wp.hwndInsertAfter != new IntPtr(HWND_TOPMOST))
                {
                    wp.hwndInsertAfter = new IntPtr(HWND_TOPMOST);
                    Marshal.StructureToPtr(wp, m.LParam, true);
                }
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == WM_DEVICECHANGE)
            {
                int wParam = m.WParam.ToInt32();
                if (wParam == DBT_DEVICEREMOVECOMPLETE && this.Visible)
                {
                    ToggleVisibility();
                }
                else if (wParam == DBT_DEVICEARRIVAL && !this.Visible)
                {
                    ToggleVisibility();
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            isRunning = false;
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
            }
            protectionThread.Join(1000);
            topmostThread.Join(1000);
            base.OnFormClosing(e);
            Environment.Exit(0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }
    }
}