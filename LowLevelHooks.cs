using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace Utility
{
    public class LowLevelHooks
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;

        private short LButtonState = 0;
        private short RButtonState = 0;
        private short MButtonState = 0;

        private bool _CheckMouseButtons = false;

        public DateTime LastEventTime = DateTime.Now;
        private LowLevelKeyboardProc _proc;
        private IntPtr _KeyboardHookID = IntPtr.Zero;

        Thread mouseThread;
        ThreadStart TStart;

        public delegate void OnKeyEventHandler(Keys Key);
        public OnKeyEventHandler OnKeyPress;

        #region Imports
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int vKey);
        #endregion


        public LowLevelHooks()
        {
            _proc = HookCallback;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc, int wParam)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(wParam, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                OnKeyPress?.Invoke((Keys)Marshal.ReadInt32(lParam));
                SetLastEventTimeToNow();
            }
            return CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }

        private void CheckMouseButtons()
        {
            bool wasKeyChanged(ref short oldKeyState, int key)
            {
                var tmpState = GetKeyState(key);
                if (tmpState != oldKeyState)
                {
                    oldKeyState = tmpState;
                    SetLastEventTimeToNow();
                    return true;
                }
                else
                    return false;
            }

            // set our defaults.
            LButtonState = GetKeyState(VK_LBUTTON);

            while (_CheckMouseButtons)
            {
                Thread.Sleep(100);
                if (wasKeyChanged(ref LButtonState, VK_LBUTTON))
                    continue;
                else
                if (wasKeyChanged(ref RButtonState, VK_RBUTTON))
                    continue;
                else
                    wasKeyChanged(ref MButtonState, VK_MBUTTON);                   
            }
        }

        public void SetLastEventTimeToNow()
        {
            LastEventTime = DateTime.Now;
        }

        public void Start()
        {
            _KeyboardHookID = SetHook(_proc, WH_KEYBOARD_LL);
            _CheckMouseButtons = true;
            TStart = new ThreadStart(CheckMouseButtons);
            mouseThread = new Thread(TStart);
            mouseThread.Start();
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_KeyboardHookID);

            _CheckMouseButtons = false;
            if (mouseThread.ThreadState == System.Threading.ThreadState.Running)
                mouseThread.Abort();
        }


    }
}
