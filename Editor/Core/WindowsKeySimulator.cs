#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClaudeCode.Editor.Core
{
    public static class WindowsConsoleInjector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateFile(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleInputW(IntPtr hConsoleInput, INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);

        const int STD_INPUT_HANDLE = -10;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x1;
        const uint FILE_SHARE_WRITE = 0x2;
        const uint OPEN_EXISTING = 3;
        const ushort KEY_EVENT = 0x0001;
        static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        struct COORD { public short X, Y; }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT_RECORD
        {
            [FieldOffset(0)] public ushort EventType;
            [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEY_EVENT_RECORD
        {
            public int bKeyDown;
            public ushort wRepeatCount;
            public ushort wVirtualKeyCode;
            public ushort wVirtualScanCode;
            public char UnicodeChar;
            public uint dwControlKeyState;
        }

        public static bool InjectTextAndEnter(int processId, string text)
        {
            try
            {
                FreeConsole();
                if (!AttachConsole((uint)processId))
                {
                    UnityEngine.Debug.LogWarning($"[ClaudeCode] AttachConsole failed: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                var hIn = CreateFile("CONIN$", GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

                if (hIn == INVALID_HANDLE)
                {
                    UnityEngine.Debug.LogWarning($"[ClaudeCode] CreateFile CONIN$ failed: {Marshal.GetLastWin32Error()}");
                    FreeConsole();
                    return false;
                }

                try
                {
                    foreach (char c in text)
                        WriteKey(hIn, c, 0);

                    WriteKey(hIn, '\r', 0x0D);
                    return true;
                }
                finally
                {
                    CloseHandle(hIn);
                    FreeConsole();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ClaudeCode] Console inject error: {e.Message}");
                try { FreeConsole(); } catch { }
                return false;
            }
        }

        static void WriteKey(IntPtr hIn, char c, ushort vk)
        {
            var records = new INPUT_RECORD[2];
            records[0] = new INPUT_RECORD
            {
                EventType = KEY_EVENT,
                KeyEvent = new KEY_EVENT_RECORD
                {
                    bKeyDown = 1,
                    wRepeatCount = 1,
                    wVirtualKeyCode = vk,
                    UnicodeChar = c
                }
            };
            records[1] = records[0];
            records[1].KeyEvent.bKeyDown = 0;
            WriteConsoleInputW(hIn, records, 2, out _);
        }
    }

    public static class WindowsKeySimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern short VkKeyScan(char ch);

        const int SW_RESTORE = 9;

        const ushort VK_CONTROL = 0x11;
        const ushort VK_V = 0x56;
        const ushort VK_RETURN = 0x0D;

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL, wParamH;
        }

        public static bool FocusAndSendPasteEnter(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            ShowWindow(hWnd, SW_RESTORE);
            if (!SetForegroundWindow(hWnd)) return false;

            Thread.Sleep(150);

            SendKeyCombo(VK_CONTROL, VK_V);
            Thread.Sleep(80);
            SendKey(VK_RETURN);

            return true;
        }

        public static bool FocusAndTypeText(IntPtr hWnd, string text)
        {
            if (hWnd == IntPtr.Zero) return false;

            ShowWindow(hWnd, SW_RESTORE);
            if (!SetForegroundWindow(hWnd)) return false;

            Thread.Sleep(200);

            foreach (char c in text)
            {
                SendUnicodeChar(c);
                Thread.Sleep(5);
            }

            Thread.Sleep(50);
            SendKey(VK_RETURN);

            return true;
        }

        static void SendUnicodeChar(char c)
        {
            var inputs = new INPUT[2];
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE
                    }
                }
            };
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                    }
                }
            };
            SendInput(2, inputs, INPUT.Size);
        }

        static void SendKey(ushort vk)
        {
            var inputs = new INPUT[2];
            inputs[0] = MakeKeyInput(vk, false);
            inputs[1] = MakeKeyInput(vk, true);
            SendInput(2, inputs, INPUT.Size);
        }

        static void SendKeyCombo(ushort modifier, ushort key)
        {
            var inputs = new INPUT[4];
            inputs[0] = MakeKeyInput(modifier, false);
            inputs[1] = MakeKeyInput(key, false);
            inputs[2] = MakeKeyInput(key, true);
            inputs[3] = MakeKeyInput(modifier, true);
            SendInput(4, inputs, INPUT.Size);
        }

        static INPUT MakeKeyInput(ushort vk, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                    }
                }
            };
        }

        public static IntPtr WaitForProcessWindow(Process process, int timeoutMs = 5000)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;
                Thread.Sleep(100);
                waited += 100;
            }
            return IntPtr.Zero;
        }
    }
}
#endif
