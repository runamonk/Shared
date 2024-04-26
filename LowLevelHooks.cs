using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ThreadState = System.Threading.ThreadState;

namespace Utility
{
    public class LowLevelHooks
    {
        public delegate void OnKeyEventHandler(Keys key);

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;
        private const int VkLbutton = 0x01;
        private const int VkRbutton = 0x02;
        private const int VkMbutton = 0x04;
        private readonly LowLevelKeyboardProc _proc;

        private bool _checkMouseButtons;
        private IntPtr _keyboardHookId = IntPtr.Zero;

        private short _lButtonState;
        private short _mButtonState;

        private Thread _mouseThread;
        private short _rButtonState;
        private ThreadStart _start;

        public DateTime LastEventTime = DateTime.Now;
        public OnKeyEventHandler OnKeyPress;


        public LowLevelHooks() { _proc = HookCallback; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private void CheckMouseButtons()
        {
            bool WasKeyChanged(ref short oldKeyState, int key)
            {
                short tmpState = GetKeyState(key);
                if (tmpState != oldKeyState)
                {
                    oldKeyState = tmpState;
                    SetLastEventTimeToNow();
                    return true;
                }

                return false;
            }

            // set our defaults.
            _lButtonState = GetKeyState(VkLbutton);

            while (_checkMouseButtons)
            {
                Thread.Sleep(100);
                if (WasKeyChanged(ref _lButtonState, VkLbutton))
                    continue;
                if (WasKeyChanged(ref _rButtonState, VkRbutton))
                    continue;
                WasKeyChanged(ref _mButtonState, VkMbutton);
            }
        }

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0 && wParam == (IntPtr)WmKeydown) || wParam == (IntPtr)WmSyskeydown)
            {
                OnKeyPress?.Invoke((Keys)Marshal.ReadInt32(lParam));
                SetLastEventTimeToNow();
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc, int wParam)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(wParam, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void SetLastEventTimeToNow() { LastEventTime = DateTime.Now; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        public void Start()
        {
            _keyboardHookId = SetHook(_proc, WhKeyboardLl);
            _checkMouseButtons = true;
            _start = CheckMouseButtons;
            _mouseThread = new Thread(_start);
            _mouseThread.Start();
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_keyboardHookId);

            _checkMouseButtons = false;
            if (_mouseThread.ThreadState == ThreadState.Running)
                _mouseThread.Abort();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}