using System;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace zuulWindowTracker
{
    class WindowTracker
    {
        // Delegate and imports from pinvoke.net:
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        // Constants from winuser.h
        const uint EVENT_SYSTEM_FOREGROUND = 3;
        const uint WINEVENT_OUTOFCONTEXT = 0;

        // Need to ensure delegate is not collected while we're using it,
        // storing it in a class field is simplest way to do this.
        WinEventDelegate procDelegate;
        IntPtr hhook;

        public delegate void WindowChangedDelegate(IntPtr hwnd);
        public event WindowChangedDelegate WindowChanged;

        public WindowTracker()
        {
            procDelegate = new WinEventDelegate(WinEventProc);
            // Listen for foreground changes across all processes/threads on current desktop...
            hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        ~WindowTracker()
        {
            UnhookWinEvent(hhook);
        }

        void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            WindowChanged(hwnd);
        }
    }
}