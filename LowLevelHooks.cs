using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ThreadState = System.Threading.ThreadState;

namespace zuul
{
    public class LowLevelHooks
    {
        public delegate void OnKeyEventHandler(Keys key);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_LMBUTTON = 0x01;
        private const int VK_RMBUTTON = 0x02;
        private const int VK_MMBUTTON = 0x04;
        
        private readonly LowLevelKeyboardProc keyboardProc;

        private bool doEventThread;
        private IntPtr keyboardHookId = IntPtr.Zero;

        private Thread eventThread;

        public DateTime LastEventTime = DateTime.Now;
        public OnKeyEventHandler OnKeyPress;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        public LowLevelHooks() 
        { 
            keyboardProc = HookCallback; 
        }

        private void doEvents()
        {
            short lButtonState = GetKeyState(VK_LMBUTTON);
            short rButtonState = GetKeyState(VK_RMBUTTON);
            short mButtonState = GetKeyState(VK_MMBUTTON);
            int cursorX = Cursor.Position.X;
            int cursorY = Cursor.Position.Y;

            bool WasKeyChanged(ref short oldKeyState, int key)
            {
                short tmpState = GetKeyState(key);
                if (tmpState != oldKeyState)
                {
                    oldKeyState = tmpState;
                    return true;
                }

                return false;
            }

            bool HasCursorMoved()
            {
                if (cursorX != Cursor.Position.X || cursorY != Cursor.Position.Y)
                {
                    cursorX = Cursor.Position.X;
                    cursorY = Cursor.Position.Y;
                    return true;
                }
                return false;
            }

            while (doEventThread)
            {
                Thread.Sleep(100);

                if (WasKeyChanged(ref lButtonState, VK_LMBUTTON) || 
                    WasKeyChanged(ref rButtonState, VK_RMBUTTON) || 
                    WasKeyChanged(ref mButtonState, VK_MMBUTTON) || 
                    HasCursorMoved())
                   SetLastEventTimeToNow();

                Application.DoEvents();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                OnKeyPress?.Invoke((Keys)Marshal.ReadInt32(lParam));
                SetLastEventTimeToNow();
            }

            return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc, int wParam)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(wParam, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void SetLastEventTimeToNow() 
        { 
            LastEventTime = DateTime.Now; 
        }

        public void Start()
        {
            keyboardHookId = SetHook(keyboardProc, WH_KEYBOARD_LL);
            doEventThread = true;

            eventThread = new Thread(new ThreadStart(doEvents));
            eventThread.Start();
        }

        public void Stop()
        {
            UnhookWindowsHookEx(keyboardHookId);

            doEventThread = false;
            if (eventThread.ThreadState == ThreadState.Running)
                eventThread.Abort();
        }
    }
}