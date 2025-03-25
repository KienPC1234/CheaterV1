using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using WindowsInput;

namespace CheaterV1
{
    internal class HoTro : IDisposable
    {
        private Dictionary<Keys, string> keyMappings;
        private bool isPaused = false;
        private Random random = new Random();
        private IKeyboardMouseEvents globalHook;
        private Keys currentKey = Keys.None;
        private CancellationTokenSource cts;
        private InputSimulator simulator;

        public HoTro()
        {
            LoadKeyMappings();
            SetupKeyboardHook();
            simulator = new InputSimulator();
        }

        private void SetupKeyboardHook()
        {
            try
            {
                globalHook = Hook.GlobalEvents();
                globalHook.KeyDown += async (sender, e) =>
                {
                    if (e.KeyCode == Keys.RMenu)
                    {
                        e.Handled = true;
                        isPaused = !isPaused;
                    }
                    else if (keyMappings != null && keyMappings.ContainsKey(e.KeyCode))
                    {
                        e.Handled = true;
                        if (currentKey != e.KeyCode)
                        {
                            if (cts != null)
                            {
                                cts.Cancel();
                                cts.Dispose();
                            }
                            currentKey = e.KeyCode;
                            isPaused = false;
                            cts = new CancellationTokenSource();
                            _ = TypeTextFromJson(e.KeyCode, cts.Token);
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadKeyMappings()
        {
            string filePath = "keys.json";
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var keyList = JsonSerializer.Deserialize<List<KeyMapping>>(json);
                keyMappings = new Dictionary<Keys, string>();
                foreach (var item in keyList)
                {
                    if (Enum.TryParse(item.keycode, out Keys key))
                    {
                        keyMappings[key] = item.text;
                    }
                }
            }
            else
            {
                MessageBox.Show("File keys.json không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task TypeTextFromJson(Keys key, CancellationToken token)
        {
            if (!keyMappings.TryGetValue(key, out string text) || string.IsNullOrEmpty(text))
            {
                currentKey = Keys.None;
                return;
            }
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int wordCount = 0;

            foreach (var word in words)
            {
                if (token.IsCancellationRequested) break;
                while (isPaused)
                {
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested) return;
                }

                foreach (char c in word)
                {
                    if (token.IsCancellationRequested) break;
                    if (char.IsUpper(c))
                    {
                        simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, CharToVirtualKeyCode(char.ToLower(c)));
                    }
                    else
                    {
                        simulator.Keyboard.KeyPress(CharToVirtualKeyCode(c));
                    }
                    await Task.Delay(random.Next(100, 200), token);
                }

                if (!token.IsCancellationRequested)
                {
                    simulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                    await Task.Delay(random.Next(200, 300), token);
                }

                wordCount++;
                if (wordCount >= random.Next(10, 18))
                {
                    await Task.Delay(random.Next(1000, 2000), token);
                    wordCount = 0;
                }
            }
            if (!token.IsCancellationRequested) currentKey = Keys.None;
        }

        private VirtualKeyCode CharToVirtualKeyCode(char c)
        {
            if (char.IsLetter(c))
            {
                return (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), "VK_" + char.ToUpper(c));
            }
            if (char.IsDigit(c))
            {
                return (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), "VK_" + c);
            }
            return c switch
            {
                ' ' => VirtualKeyCode.SPACE,
                '+' => VirtualKeyCode.OEM_PLUS,
                '-' => VirtualKeyCode.OEM_MINUS,
                ',' => VirtualKeyCode.OEM_COMMA,
                '.' => VirtualKeyCode.OEM_PERIOD,
                '/' => VirtualKeyCode.OEM_2,
                ';' => VirtualKeyCode.OEM_1,
                '=' => VirtualKeyCode.OEM_PLUS,
                '[' => VirtualKeyCode.OEM_4,
                ']' => VirtualKeyCode.OEM_6,
                '\\' => VirtualKeyCode.OEM_5,
                '\'' => VirtualKeyCode.OEM_7,
                _ => VirtualKeyCode.SPACE
            };
        }

        public void Dispose()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
            globalHook?.Dispose();
        }

        private class KeyMapping
        {
            public string keycode { get; set; }
            public string text { get; set; }
        }
    }
}