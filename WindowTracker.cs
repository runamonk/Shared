using System;
using System.Runtime.InteropServices;

namespace zuulWindowTracker
{
    internal class WindowTracker
    {
        public delegate void WindowChangedDelegate(IntPtr hwnd);

        // Constants from winuser.h
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private readonly IntPtr hhook;

        // Need to ensure delegate is not collected while we're using it,
        // storing it in a class field is simplest way to do this.
        private readonly WinEventDelegate procDelegate;

        public WindowTracker()
        {
            procDelegate = WinEventProc;
            // Listen for foreground changes across all processes/threads on current desktop...
            hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, procDelegate, 0, 0,
                WINEVENT_OUTOFCONTEXT);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        public event WindowChangedDelegate WindowChanged;

        ~WindowTracker()
        {
            UnhookWinEvent(hhook);
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            WindowChanged(hwnd);
        }

        // Delegate and imports from pinvoke.net:
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
            int idChild, uint dwEventThread, uint dwmsEventTime);
    }
}