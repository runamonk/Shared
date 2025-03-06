using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ThreadState = System.Threading.ThreadState;

namespace zuul
{
    public class LowLevelHooks
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public delegate void LastEventTimeUpdatedHandler();
        public event LastEventTimeUpdatedHandler OnLastEventTimeUpdated;

        public delegate void KeyWasPressedHandler();
        public event KeyWasPressedHandler OnKeyWasPressed;

        public delegate void MouseWasDiddledHandler();
        public event MouseWasDiddledHandler OnMouseWasDiddled;

        private bool doMouseThread = false;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_LMBUTTON = 0x01;
        private const int VK_RMBUTTON = 0x02;
        private const int VK_MMBUTTON = 0x04;
        private const string activeChars = "abcdefghijklmnopqrstuvwxyz0123456789!£$~¬`{}[],.<>/?_+-=";

        private readonly LowLevelKeyboardProc keyboardProc;

        private IntPtr keyboardHookId = IntPtr.Zero;
        private Thread mouseActivityThread;

        public DateTime LastEventTime = DateTime.Now;
        public DateTime LastKeyEventTime = DateTime.Now;
        public DateTime LastMouseEventTime = DateTime.Now;
        
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

        private void checkMouseActivity()
        {
            short lButtonState = GetKeyState(VK_LMBUTTON);
            short rButtonState = GetKeyState(VK_RMBUTTON);
            short mButtonState = GetKeyState(VK_MMBUTTON);
            int cursorX = Cursor.Position.X;
            int cursorY = Cursor.Position.Y;

            bool WasMouseClicked(ref short oldKeyState, int key)
            {
                short tmpState = GetKeyState(key);
                if (tmpState != oldKeyState)
                {
                    oldKeyState = tmpState;
                    SetLastMouseEventTimeToNow();
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
                    SetLastMouseEventTimeToNow();
                    return true;
                }
                return false;
            }

            while (doMouseThread)
            {
                Thread.Sleep(100);

                if (WasMouseClicked(ref lButtonState, VK_LMBUTTON) ||
                    WasMouseClicked(ref rButtonState, VK_RMBUTTON) ||
                    WasMouseClicked(ref mButtonState, VK_MMBUTTON) || 
                    HasCursorMoved())
                   SetLastMouseEventTimeToNow();

                Application.DoEvents();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && (activeChars.IndexOf(((Keys)Marshal.ReadInt32(lParam)).ToString().ToLower()) > -1))
            {          
                SetLastKeyEventTimeToNow();
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
            OnLastEventTimeUpdated?.Invoke();
        }

        public void SetLastKeyEventTimeToNow()
        {
            LastKeyEventTime = DateTime.Now;
            OnKeyWasPressed?.Invoke();
            SetLastEventTimeToNow();
        }

        public void SetLastMouseEventTimeToNow()
        {
            LastMouseEventTime = DateTime.Now;
            OnMouseWasDiddled?.Invoke();
            SetLastEventTimeToNow();
        }

        public void Start()
        {
            keyboardHookId = SetHook(keyboardProc, WH_KEYBOARD_LL);
            doMouseThread = true;
            mouseActivityThread = new Thread(new ThreadStart(checkMouseActivity));
            mouseActivityThread.Start();
        }

        public void Stop()
        {
            doMouseThread = false;
            UnhookWindowsHookEx(keyboardHookId);
            if (mouseActivityThread.ThreadState == ThreadState.Running)
                mouseActivityThread.Abort();
        }
    }
}